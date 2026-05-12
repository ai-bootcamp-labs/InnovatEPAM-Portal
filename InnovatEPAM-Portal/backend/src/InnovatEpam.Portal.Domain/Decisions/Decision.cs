using InnovatEpam.Portal.Domain.Enums;

namespace InnovatEpam.Portal.Domain.Decisions;

/// <summary>
/// Point-in-time admin action against an idea (FR-018, FR-019, FR-021).
/// See data-model §6.
/// </summary>
/// <remarks>Phase 2 stub — Phase 4 (US2) refines.</remarks>
public class Decision
{
    public Guid Id { get; set; }
    public Guid IdeaId { get; set; }
    public DecisionAction Action { get; set; }
    public string? Comment { get; set; }
    public Guid DecidedById { get; set; }
    public DateTimeOffset DecidedAt { get; set; }
}
