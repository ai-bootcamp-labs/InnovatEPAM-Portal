using System.Text.Json;
using InnovatEpam.Portal.Application.Decisions.Dtos;
using InnovatEpam.Portal.Application.Ideas;
using InnovatEpam.Portal.Application.Ideas.Dtos;
using InnovatEpam.Portal.Application.Persistence;
using InnovatEpam.Portal.Domain.Decisions;
using InnovatEpam.Portal.Domain.Enums;
using InnovatEpam.Portal.Domain.Exceptions;
using InnovatEpam.Portal.Domain.Ideas;
using InnovatEpam.Portal.Domain.Notifications;
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
        //
        // Notes:
        //  • `xmin` is a PostgreSQL system column and is NOT included by `select *`.
        //    Because IdeaConfiguration maps RowVersion → xmin (xid), the row must
        //    be projected explicitly or EF's materializer fails the request.
        //  • AsNoTracking is intentional: the AFTER-INSERT trigger
        //    `trg_decision_after_insert` is the single writer for ideas.status,
        //    last_decision_id, updated_at, and the idea_status_history row.
        //    Tracking the entity here would cause EF to also UPDATE ideas, racing
        //    the trigger and producing spurious DbUpdateConcurrencyException 409s.
        //    The C# TransitionTo(...) call below stays purely as a validation
        //    gate that surfaces terminal-status / no-op transitions as 409.
        var idea = await _db.Ideas
            .FromSqlInterpolated($"select *, xmin from portal.ideas where id = {ideaId} for update")
            .AsNoTracking()
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("Idea not found.");

        // Throws ConflictException on illegal transitions (terminal status, no-op).
        var fromStatus = idea.Status;
        try
        {
            idea.TransitionTo(nextStatus, request.Action);
        }
        catch (ConflictException)
        {
            _logger.LogWarning("decision.conflict {IdeaId} {AdminId}", ideaId, adminId);
            throw;
        }

        // Phase 6 / FR-004: capture whether blind-review mode was active at
        // decision time so the audit trail stays honest even if blind mode is
        // later disabled. Blind mode applies whenever the idea was not already
        // terminal at the moment of decision (i.e. for the very first
        // Accept/Reject — the call that *causes* identity to be revealed).
        var wasBlind = fromStatus is not (IdeaStatus.Accepted or IdeaStatus.Rejected);
        var decision = Decision.Create(idea.Id, request.Action, request.Comment, adminId, DateTimeOffset.UtcNow, wasBlind);
        _db.Decisions.Add(decision);

        // T102 — emit an in-portal notification for the submitter inside the
        // same transaction so the unread count reflects reality immediately.
        var actor = await _db.Users.AsNoTracking()
            .Where(u => u.Id == adminId)
            .Select(u => new { u.DisplayName })
            .FirstOrDefaultAsync(ct);
        var payload = JsonSerializer.Serialize(new
        {
            ideaId = idea.Id,
            ideaTitle = idea.Title,
            fromStatus = fromStatus.ToString(),
            toStatus = nextStatus.ToString(),
            decidedById = adminId,
            decidedByDisplayName = actor?.DisplayName ?? string.Empty,
        });
        var notification = Notification.Create(
            recipientId: idea.SubmitterId,
            ideaId: idea.Id,
            kind: Notification.KindIdeaStatusChanged,
            payloadJson: payload,
            createdAt: decision.DecidedAt);
        _db.Notifications.Add(notification);

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
        return await _ideas.GetByIdAsync(ideaId, ct, callerId: adminId, callerIsAdmin: true);
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
