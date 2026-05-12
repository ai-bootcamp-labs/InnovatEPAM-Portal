using InnovatEpam.Portal.Application.Ideas.Dtos;
using InnovatEpam.Portal.Application.Persistence;
using InnovatEpam.Portal.Domain.Enums;
using InnovatEpam.Portal.Domain.Exceptions;
using InnovatEpam.Portal.Domain.Ideas;
using Microsoft.EntityFrameworkCore;

namespace InnovatEpam.Portal.Application.Ideas;

/// <summary>
/// Idea aggregate use-cases (T063): create, list, fetch (FR-009..014, FR-021).
/// </summary>
public sealed class IdeaService
{
    private readonly IPortalDbContext _db;

    public IdeaService(IPortalDbContext db)
    {
        _db = db;
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

        return await GetByIdAsync(idea.Id, ct);
    }

    /// <summary>Returns a single idea projection or throws <see cref="NotFoundException"/>.</summary>
    public async Task<IdeaDetail> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var detail = await BuildDetailQuery(_db.Ideas.AsNoTracking().Where(i => i.Id == id))
            .FirstOrDefaultAsync(ct);
        return detail ?? throw new NotFoundException("Idea not found.");
    }

    /// <summary>
    /// Listing with filtering + pagination (FR-013, FR-014, US3 entry-point, T096).
    /// </summary>
    /// <remarks>
    /// <paramref name="sort"/> accepts <c>createdAt</c>, <c>-createdAt</c> (default),
    /// <c>updatedAt</c>, <c>-updatedAt</c>, <c>title</c>, <c>-title</c>. Unknown values
    /// fall back to the default to keep the index <c>ix_idea_status_created_at</c> hot.
    /// </remarks>
    public async Task<PagedIdeas> ListAsync(
        IdeaStatus? status,
        string? categoryCode,
        Guid? submitterId,
        int page,
        int pageSize,
        CancellationToken ct,
        string? sort = null)
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

        var ordered = ApplySort(query, sort);

        var slice = ordered
            .Skip((page - 1) * pageSize)
            .Take(pageSize);

        var items = await (
            from i in slice
            join c in _db.Categories on i.CategoryId equals c.Id
            join u in _db.Users on i.SubmitterId equals u.Id
            select new IdeaListItem(
                i.Id,
                i.Title,
                c.Id,
                c.Name,
                i.Status,
                u.Id,
                u.DisplayName,
                _db.Attachments.Any(a => a.IdeaId == i.Id),
                i.CreatedAt,
                i.UpdatedAt))
            .ToListAsync(ct);

        return new PagedIdeas(items, total, page, pageSize);
    }

    private static IQueryable<Idea> ApplySort(IQueryable<Idea> query, string? sort) => sort switch
    {
        "createdAt" => query.OrderBy(i => i.CreatedAt),
        "updatedAt" => query.OrderBy(i => i.UpdatedAt),
        "-updatedAt" => query.OrderByDescending(i => i.UpdatedAt),
        "title" => query.OrderBy(i => i.Title),
        "-title" => query.OrderByDescending(i => i.Title),
        _ => query.OrderByDescending(i => i.CreatedAt),
    };

    private IQueryable<IdeaDetail> BuildDetailQuery(IQueryable<Idea> ideas) =>
        from i in ideas
        join c in _db.Categories on i.CategoryId equals c.Id
        join u in _db.Users on i.SubmitterId equals u.Id
        let att = _db.Attachments.FirstOrDefault(a => a.IdeaId == i.Id)
        let lastDecision = _db.Decisions
            .Where(d => d.IdeaId == i.Id)
            .OrderByDescending(d => d.DecidedAt)
            .FirstOrDefault()
        let lastDecider = lastDecision == null ? null : _db.Users.FirstOrDefault(x => x.Id == lastDecision.DecidedById)
        select new IdeaDetail(
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
            i.CreatedAt,
            i.UpdatedAt);
}
