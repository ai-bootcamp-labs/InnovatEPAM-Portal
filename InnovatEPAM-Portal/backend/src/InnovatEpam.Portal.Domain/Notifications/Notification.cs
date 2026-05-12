namespace InnovatEpam.Portal.Domain.Notifications;

/// <summary>
/// Per-user in-portal notification (FR-022, R14). See data-model §8.
/// </summary>
/// <remarks>Phase 2 stub — Phase 5 (US3) refines.</remarks>
public class Notification
{
    public Guid Id { get; set; }
    public Guid RecipientId { get; set; }
    public Guid? IdeaId { get; set; }
    public string Kind { get; set; } = string.Empty;
    public string Payload { get; set; } = "{}";
    public DateTimeOffset? ReadAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
