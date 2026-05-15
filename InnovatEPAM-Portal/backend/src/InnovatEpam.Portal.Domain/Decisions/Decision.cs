using InnovatEpam.Portal.Domain.Enums;
using InnovatEpam.Portal.Domain.Exceptions;

namespace InnovatEpam.Portal.Domain.Decisions;

/// <summary>
/// Point-in-time admin action against an idea (FR-018, FR-019, FR-021).
/// Constructor enforces FR-019: comment is required for Accept/Reject.
/// See data-model §6.
/// </summary>
/// <remarks>
/// Phase 6 added <see cref="WasBlind"/>: <c>true</c> if blind-review mode was
/// active at decision time (FR-004). The audit trail keeps this even if blind
/// mode is later disabled.
/// </remarks>
public sealed class Decision
{
    public const int CommentMaxLength = 2000;

    private Decision() { }

    private Decision(Guid id, Guid ideaId, DecisionAction action, string? comment, Guid decidedById, DateTimeOffset decidedAt, bool wasBlind)
    {
        Id = id;
        IdeaId = ideaId;
        Action = action;
        Comment = comment;
        DecidedById = decidedById;
        DecidedAt = decidedAt;
        WasBlind = wasBlind;
    }

    public Guid Id { get; private set; }
    public Guid IdeaId { get; private set; }
    public DecisionAction Action { get; private set; }
    public string? Comment { get; private set; }
    public Guid DecidedById { get; private set; }
    public DateTimeOffset DecidedAt { get; private set; }

    /// <summary>
    /// True iff blind-review mode was active when the decision was recorded.
    /// Persisted as <c>boolean NOT NULL DEFAULT false</c>.
    /// </summary>
    public bool WasBlind { get; private set; }

    public static Decision Create(
        Guid ideaId,
        DecisionAction action,
        string? comment,
        Guid decidedById,
        DateTimeOffset decidedAt,
        bool wasBlind = false)
    {
        var trimmed = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim();
        if (action is DecisionAction.Accept or DecisionAction.Reject)
        {
            if (string.IsNullOrEmpty(trimmed))
            {
                throw new DomainValidationException("Comment is required for Accept/Reject.",
                    new Dictionary<string, string[]> { ["Comment"] = ["Comment is required for Accept/Reject."] });
            }
            if (trimmed.Length > CommentMaxLength)
            {
                throw new DomainValidationException("Comment too long.",
                    new Dictionary<string, string[]> { ["Comment"] = [$"Comment must be ≤ {CommentMaxLength} characters."] });
            }
        }
        return new Decision(Guid.NewGuid(), ideaId, action, trimmed, decidedById, decidedAt, wasBlind);
    }
}
