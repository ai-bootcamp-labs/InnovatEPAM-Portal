using InnovatEpam.Portal.Domain.Enums;

namespace InnovatEpam.Portal.Domain.Ideas;

/// <summary>
/// Append-only audit row for idea status transitions (FR-021).
/// See data-model §7. Inserted by the aggregate creator (initial row, from = NULL,
/// to = Submitted) and by the <c>trg_decision_after_insert</c> trigger.
/// </summary>
/// <remarks>Phase 2 stub — Phase 3 (T057) refines.</remarks>
public class IdeaStatusHistory
{
    public Guid Id { get; set; }
    public Guid IdeaId { get; set; }
    public IdeaStatus? FromStatus { get; set; }
    public IdeaStatus ToStatus { get; set; }
    public Guid ActorId { get; set; }
    public string? Comment { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
    public Guid? DecisionId { get; set; }
}
