namespace InnovatEpam.Portal.Domain.Outbox;

/// <summary>
/// Reserved for Phase 2-of-overall-roadmap email/SignalR fan-out. Created by
/// migration but not read by the Phase 1 MVP (data-model §9).
/// </summary>
public class OutboxMessage
{
    public Guid Id { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Payload { get; set; } = "{}";
    public DateTimeOffset? ProcessedAt { get; set; }
}
