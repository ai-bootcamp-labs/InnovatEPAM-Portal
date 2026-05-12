using InnovatEpam.Portal.Application.Decisions.Dtos;
using InnovatEpam.Portal.Application.Ideas;
using InnovatEpam.Portal.Application.Ideas.Dtos;
using InnovatEpam.Portal.Application.Persistence;
using InnovatEpam.Portal.Domain.Decisions;
using InnovatEpam.Portal.Domain.Enums;
using InnovatEpam.Portal.Domain.Exceptions;
using InnovatEpam.Portal.Domain.Ideas;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace InnovatEpam.Portal.Application.Decisions;

/// <summary>
/// Records admin decisions against ideas (T085, FR-005, FR-018, FR-019, FR-020, FR-021).
/// Uses <c>SELECT … FOR UPDATE</c> to serialise concurrent admins on the same idea
/// and surface terminal-status conflicts as <see cref="ConflictException"/> (HTTP 409).
/// The <c>trg_decision_after_insert</c> trigger (T035) updates the idea status,
/// pointer, and inserts the corresponding history row inside the same transaction.
/// </summary>
public sealed class DecisionService
{
    private readonly IPortalDbContext _db;
    private readonly IdeaService _ideas;
    private readonly ILogger<DecisionService> _logger;

    public DecisionService(IPortalDbContext db, IdeaService ideas, ILogger<DecisionService> logger)
    {
        _db = db;
        _ideas = ideas;
        _logger = logger;
    }

    public async Task<IdeaDetail> RecordAsync(
        Guid ideaId,
        CreateDecisionRequest request,
        Guid adminId,
        CancellationToken ct)
    {
        var nextStatus = MapNextStatus(request.Action);

        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        // Pessimistic row lock — second concurrent admin blocks until the first
        // commits, then re-reads the up-to-date status (FR-020).
        var idea = await _db.Ideas
            .FromSqlInterpolated($"select * from portal.ideas where id = {ideaId} for update")
            .AsTracking()
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("Idea not found.");

        // Throws ConflictException on illegal transitions (terminal status, no-op).
        idea.TransitionTo(nextStatus, request.Action);

        var decision = Decision.Create(idea.Id, request.Action, request.Comment, adminId, DateTimeOffset.UtcNow);
        idea.AssignLastDecision(decision.Id);
        idea.UpdatedAt = decision.DecidedAt;
        _db.Decisions.Add(decision);

        try
        {
            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex, "decision.concurrency_conflict {IdeaId}", ideaId);
            throw new ConflictException("Idea was modified by another user. Please refresh and try again.");
        }

        _logger.LogInformation("decision.recorded {IdeaId} {Action} {AdminId}", ideaId, request.Action, adminId);
        return await _ideas.GetByIdAsync(ideaId, ct);
    }

    public async Task<IReadOnlyList<StatusHistoryEntry>> GetHistoryAsync(Guid ideaId, CancellationToken ct)
    {
        var exists = await _db.Ideas.AsNoTracking().AnyAsync(i => i.Id == ideaId, ct);
        if (!exists) throw new NotFoundException("Idea not found.");

        return await (
            from h in _db.IdeaStatusHistory.AsNoTracking()
            where h.IdeaId == ideaId
            join u in _db.Users on h.ActorId equals u.Id
            orderby h.OccurredAt
            select new StatusHistoryEntry(
                h.Id, h.FromStatus, h.ToStatus, h.ActorId, u.DisplayName,
                h.Comment, h.DecisionId, h.OccurredAt))
            .ToListAsync(ct);
    }

    private static IdeaStatus MapNextStatus(DecisionAction action) => action switch
    {
        DecisionAction.MoveToUnderReview => IdeaStatus.UnderReview,
        DecisionAction.Accept => IdeaStatus.Accepted,
        DecisionAction.Reject => IdeaStatus.Rejected,
        _ => throw new DomainValidationException("Unknown decision action.",
            new Dictionary<string, string[]> { ["Action"] = ["Unknown decision action."] }),
    };
}
