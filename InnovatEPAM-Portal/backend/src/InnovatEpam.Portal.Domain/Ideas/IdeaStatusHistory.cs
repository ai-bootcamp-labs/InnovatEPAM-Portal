using InnovatEpam.Portal.Domain.Enums;

namespace InnovatEpam.Portal.Domain.Ideas;

/// <summary>
/// Append-only audit row for idea status transitions (FR-021). See data-model §7.
/// Inserted by <see cref="Idea"/> on initial creation (FromStatus = NULL,
/// ToStatus = Submitted) and by the <c>trg_decision_after_insert</c> trigger
/// for subsequent transitions.
/// </summary>
public sealed class IdeaStatusHistory
{
    private IdeaStatusHistory() { }

    private IdeaStatusHistory(
        Guid id,
        Guid ideaId,
        IdeaStatus? from,
        IdeaStatus to,
        Guid actorId,
        string? comment,
        Guid? decisionId,
        DateTimeOffset occurredAt)
    {
        Id = id;
        IdeaId = ideaId;
        FromStatus = from;
        ToStatus = to;
        ActorId = actorId;
        Comment = comment;
        DecisionId = decisionId;
        OccurredAt = occurredAt;
    }

    public Guid Id { get; private set; }
    public Guid IdeaId { get; private set; }
    public IdeaStatus? FromStatus { get; private set; }
    public IdeaStatus ToStatus { get; private set; }
    public Guid ActorId { get; private set; }
    public string? Comment { get; private set; }
    public DateTimeOffset OccurredAt { get; private set; }
    public Guid? DecisionId { get; private set; }

    /// <summary>Initial-creation history row (Submitter → Submitted).</summary>
    public static IdeaStatusHistory Initial(Guid ideaId, Guid submitterId, DateTimeOffset occurredAt) =>
        new(Guid.NewGuid(), ideaId, null, IdeaStatus.Submitted, submitterId, null, null, occurredAt);
}
