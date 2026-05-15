using InnovatEpam.Portal.Application.Auth;
using InnovatEpam.Portal.Application.Ideas.Dtos;
using InnovatEpam.Portal.Application.Persistence;
using InnovatEpam.Portal.Application.Scoring;
using InnovatEpam.Portal.Application.Scoring.Dtos;
using InnovatEpam.Portal.Domain.Enums;
using InnovatEpam.Portal.Domain.Exceptions;
using InnovatEpam.Portal.Domain.Ideas;
using Microsoft.EntityFrameworkCore;

namespace InnovatEpam.Portal.Application.Ideas;

/// <summary>
/// Idea aggregate use-cases (T063): create, list, fetch (FR-009..014, FR-021).
/// Phase 6 added admin-side identity redaction; Phase 7 added score aggregates
/// + opt-in score sort on the list endpoint.
/// </summary>
public sealed class IdeaService
{
    private readonly IPortalDbContext _db;
    private readonly IAliasService _alias;
    private readonly ScoringService _scoring;

    public IdeaService(IPortalDbContext db, IAliasService alias, ScoringService scoring)
    {
        _db = db;
        _alias = alias;
        _scoring = scoring;
    }

    /// <summary>
    /// Persists a new idea and its initial <see cref="IdeaStatusHistory"/> row in
    /// a single transaction (FR-021).
    /// </summary>
    public async Task<IdeaDetail> CreateAsync(CreateIdeaRequest request, Guid submitterId, CancellationToken ct)
    {
        var category = await _db.Categories.FirstOrDefaultAsync(c => c.Id == request.CategoryId, ct)
            ?? throw new NotFoundException("Category not found.");
        if (!category.IsActive)
        {
            throw new ConflictException("Category is not active.");
        }

        var idea = Idea.Create(request.Title, request.Description, category.Id, submitterId);
        var now = DateTimeOffset.UtcNow;
        idea.CreatedAt = now;
        idea.UpdatedAt = now;
        var history = IdeaStatusHistory.Initial(idea.Id, submitterId, now);

        _db.Ideas.Add(idea);
        _db.IdeaStatusHistory.Add(history);
        await _db.SaveChangesAsync(ct);

        return await GetByIdAsync(idea.Id, ct, callerId: submitterId, callerIsAdmin: false);
    }

    /// <summary>
    /// Returns a single idea projection or throws <see cref="NotFoundException"/>.
    /// Pass the caller's identity + role so Phase 6 redaction can be applied.
    /// </summary>
    public async Task<IdeaDetail> GetByIdAsync(
        Guid id,
        CancellationToken ct,
        Guid? callerId = null,
        bool callerIsAdmin = false)
    {
        var raw = await BuildRawDetailQuery(_db.Ideas.AsNoTracking().Where(i => i.Id == id))
            .FirstOrDefaultAsync(ct);
        if (raw is null) throw new NotFoundException("Idea not found.");

        var scores = await _scoring.BuildAggregateAsync(id, ct);
        return Project(raw, callerId, callerIsAdmin, scores);
    }

    /// <summary>
    /// Listing with filtering + pagination (FR-013, FR-014, US3 entry-point, T096).
    /// </summary>
    /// <remarks>
    /// <paramref name="sort"/> accepts <c>createdAt</c>, <c>-createdAt</c> (default),
    /// <c>updatedAt</c>, <c>-updatedAt</c>, <c>title</c>, <c>-title</c>, plus the
    /// Phase 7 additions <c>score:asc</c> / <c>score:desc</c> (FR-013). Unknown
    /// values fall back to the default to keep the index <c>ix_idea_status_created_at</c> hot.
    /// </remarks>
    public async Task<PagedIdeas> ListAsync(
        IdeaStatus? status,
        string? categoryCode,
        Guid? submitterId,
        int page,
        int pageSize,
        CancellationToken ct,
        string? sort = null,
        Guid? callerId = null,
        bool callerIsAdmin = false)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize switch { < 1 => 10, > 100 => 100, _ => pageSize };

        var query = _db.Ideas.AsNoTracking().AsQueryable();
        if (status.HasValue) query = query.Where(i => i.Status == status.Value);
        if (submitterId.HasValue) query = query.Where(i => i.SubmitterId == submitterId.Value);
        if (!string.IsNullOrWhiteSpace(categoryCode))
        {
            var code = categoryCode.Trim().ToLowerInvariant();
            query = query.Where(i => _db.Categories.Any(c => c.Id == i.CategoryId && c.Code == code));
        }

        var total = await query.CountAsync(ct);

        // Phase 7 — apply sort on the source IQueryable<Idea> BEFORE projecting.
        // EF Core 8 translates scalar subqueries inside OrderBy fine, but cannot
        // trace member access through a complex record constructor in a later
        // sort clause (it collapses `r => r.CreatedAt` into the whole `new
        // ListRow(...)` expression and gives up).
        var sorted = ApplySort(query, sort);

        // Pre-aggregated per-idea score projection — single SQL pass, no N+1 (FR-014).
        // Each AVG/COUNT is an inlined scalar subquery; nullable selectors map
        // empty AVG()->NULL cleanly into `double?`. We deliberately do NOT
        // bind the IdeaScores collection to a `let` symbol because EF Core 8
        // refuses to compile queries whose final projection references an
        // IQueryable<T> directly (it sees the symbol as a collection result).
        var projected =
            from i in sorted
            join c in _db.Categories on i.CategoryId equals c.Id
            join u in _db.Users on i.SubmitterId equals u.Id
            select new ListRow(
                i.Id, i.Title, c.Id, c.Name, i.Status, i.SubmitterId, u.DisplayName,
                _db.Attachments.Any(a => a.IdeaId == i.Id),
                i.CreatedAt, i.UpdatedAt,
                _db.Decisions
                    .Where(d => d.IdeaId == i.Id)
                    .OrderByDescending(d => d.DecidedAt)
                    .Select(d => (DecisionAction?)d.Action)
                    .FirstOrDefault(),
                (_db.IdeaScores.Where(s => s.IdeaId == i.Id).Average(s => (double?)s.Impact)
                 + _db.IdeaScores.Where(s => s.IdeaId == i.Id).Average(s => (double?)s.Feasibility)
                 + _db.IdeaScores.Where(s => s.IdeaId == i.Id).Average(s => (double?)s.Innovation)
                 + _db.IdeaScores.Where(s => s.IdeaId == i.Id).Average(s => (double?)s.Alignment)) / 4d,
                _db.IdeaScores.Count(s => s.IdeaId == i.Id));

        var rows = await projected.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);

        var items = rows.Select(p =>
        {
            var isBlind = ShouldHideIdentity(p.LastDecisionAction, p.SubmitterId, callerId, callerIsAdmin);
            return new IdeaListItem(
                p.Id,
                p.Title,
                p.CategoryId,
                p.CategoryName,
                p.Status,
                p.SubmitterId,
                isBlind ? null : p.SubmitterName,
                p.HasAttachment,
                p.CreatedAt,
                p.UpdatedAt,
                SubmitterAlias: isBlind ? _alias.SubmitterAlias(p.Id) : null,
                Overall: p.Overall.HasValue ? Math.Round(p.Overall.Value, 2) : (double?)null,
                ReviewerCount: p.ReviewerCount);
        }).ToList();

        return new PagedIdeas(items, total, page, pageSize);
    }

    private IOrderedQueryable<Idea> ApplySort(IQueryable<Idea> query, string? sort) => sort switch
    {
        "createdAt" => query.OrderBy(i => i.CreatedAt),
        "updatedAt" => query.OrderBy(i => i.UpdatedAt),
        "-updatedAt" => query.OrderByDescending(i => i.UpdatedAt),
        "title" => query.OrderBy(i => i.Title),
        "-title" => query.OrderByDescending(i => i.Title),
        // Phase 7 — NULLS LAST under both directions; tie-breaker = CreatedAt ASC.
        "score:desc" => query
            .OrderBy(i => !_db.IdeaScores.Any(s => s.IdeaId == i.Id))
            .ThenByDescending(i =>
                (_db.IdeaScores.Where(s => s.IdeaId == i.Id).Average(s => (double?)s.Impact)
                 + _db.IdeaScores.Where(s => s.IdeaId == i.Id).Average(s => (double?)s.Feasibility)
                 + _db.IdeaScores.Where(s => s.IdeaId == i.Id).Average(s => (double?)s.Innovation)
                 + _db.IdeaScores.Where(s => s.IdeaId == i.Id).Average(s => (double?)s.Alignment)) / 4d)
            .ThenBy(i => i.CreatedAt),
        "score:asc" => query
            .OrderBy(i => !_db.IdeaScores.Any(s => s.IdeaId == i.Id))
            .ThenBy(i =>
                (_db.IdeaScores.Where(s => s.IdeaId == i.Id).Average(s => (double?)s.Impact)
                 + _db.IdeaScores.Where(s => s.IdeaId == i.Id).Average(s => (double?)s.Feasibility)
                 + _db.IdeaScores.Where(s => s.IdeaId == i.Id).Average(s => (double?)s.Innovation)
                 + _db.IdeaScores.Where(s => s.IdeaId == i.Id).Average(s => (double?)s.Alignment)) / 4d)
            .ThenBy(i => i.CreatedAt),
        _ => query.OrderByDescending(i => i.CreatedAt),
    };

    /// <summary>Internal flat projection used between the SQL query and the materialized DTO.</summary>
    private sealed record ListRow(
        Guid Id,
        string Title,
        Guid CategoryId,
        string CategoryName,
        IdeaStatus Status,
        Guid SubmitterId,
        string SubmitterName,
        bool HasAttachment,
        DateTimeOffset CreatedAt,
        DateTimeOffset UpdatedAt,
        DecisionAction? LastDecisionAction,
        double? Overall,
        int ReviewerCount);

    /// <summary>
    /// Builds the un-redacted, score-less detail tuple. Identity redaction and
    /// score aggregation are layered on top in <see cref="Project"/>.
    /// </summary>
    private IQueryable<RawIdeaDetail> BuildRawDetailQuery(IQueryable<Idea> ideas) =>
        from i in ideas
        join c in _db.Categories on i.CategoryId equals c.Id
        join u in _db.Users on i.SubmitterId equals u.Id
        let att = _db.Attachments.FirstOrDefault(a => a.IdeaId == i.Id)
        let lastDecision = _db.Decisions
            .Where(d => d.IdeaId == i.Id)
            .OrderByDescending(d => d.DecidedAt)
            .FirstOrDefault()
        let lastDecider = lastDecision == null ? null : _db.Users.FirstOrDefault(x => x.Id == lastDecision.DecidedById)
        select new RawIdeaDetail(
            i.Id,
            i.Title,
            i.Description,
            c.Id,
            c.Name,
            i.Status,
            u.Id,
            u.DisplayName,
            att == null
                ? null
                : new AttachmentSummary(att.Id, att.OriginalFileName, att.ContentType, att.SizeBytes, att.UploadedAt),
            lastDecision == null ? null : lastDecision.Comment,
            lastDecision == null ? (Guid?)null : lastDecision.DecidedById,
            lastDecider == null ? null : lastDecider.DisplayName,
            lastDecision == null ? (DateTimeOffset?)null : lastDecision.DecidedAt,
            lastDecision == null ? (DecisionAction?)null : lastDecision.Action,
            i.CreatedAt,
            i.UpdatedAt);

    private IdeaDetail Project(RawIdeaDetail raw, Guid? callerId, bool callerIsAdmin, IdeaScoreAggregateDto scores)
    {
        var isBlind = ShouldHideIdentity(raw.LastDecisionAction, raw.SubmitterId, callerId, callerIsAdmin);
        return new IdeaDetail(
            raw.Id,
            raw.Title,
            raw.Description,
            raw.CategoryId,
            raw.CategoryName,
            raw.Status,
            raw.SubmitterId,
            isBlind ? null : raw.SubmitterName,
            raw.Attachment,
            raw.LastDecisionComment,
            raw.LastDecisionById,
            raw.LastDecisionByName,
            raw.LastDecisionAt,
            raw.CreatedAt,
            raw.UpdatedAt,
            SubmitterAlias: isBlind ? _alias.SubmitterAlias(raw.Id) : null,
            Scores: scores);
    }

    /// <summary>
    /// FR-001/FR-003: hide submitter identity iff (caller is admin) AND
    /// (latest decision is not terminal) AND (caller is not the submitter
    /// themselves — who always sees their own identity).
    /// </summary>
    internal static bool ShouldHideIdentity(
        DecisionAction? latestDecision,
        Guid submitterId,
        Guid? callerId,
        bool callerIsAdmin)
    {
        if (!callerIsAdmin) return false;
        if (callerId is not null && callerId.Value == submitterId) return false;
        return latestDecision is not (DecisionAction.Accept or DecisionAction.Reject);
    }

    private sealed record RawIdeaDetail(
        Guid Id,
        string Title,
        string Description,
        Guid CategoryId,
        string CategoryName,
        IdeaStatus Status,
        Guid SubmitterId,
        string SubmitterName,
        AttachmentSummary? Attachment,
        string? LastDecisionComment,
        Guid? LastDecisionById,
        string? LastDecisionByName,
        DateTimeOffset? LastDecisionAt,
        DecisionAction? LastDecisionAction,
        DateTimeOffset CreatedAt,
        DateTimeOffset UpdatedAt);
}
