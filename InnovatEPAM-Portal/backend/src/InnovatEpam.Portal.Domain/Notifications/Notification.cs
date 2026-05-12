namespace InnovatEpam.Portal.Domain.Notifications;

/// <summary>
/// Per-user in-portal notification (FR-022, R14). See data-model §8.
/// Phase 6 (T101) finalises the factory + kind constants used by the
/// decision-recorded write path (T102) and the polling endpoints (T103).
/// </summary>
public sealed class Notification
{
    /// <summary>Notification kind raised when an idea transitions status.</summary>
    public const string KindIdeaStatusChanged = "idea_status_changed";

    /// <summary>Maximum length of <see cref="Kind"/> (matches the EF configuration).</summary>
    public const int KindMaxLength = 64;

    public Guid Id { get; private set; }
    public Guid RecipientId { get; private set; }
    public Guid? IdeaId { get; private set; }
    public string Kind { get; private set; } = string.Empty;
    public string Payload { get; private set; } = "{}";
    public DateTimeOffset? ReadAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    // EF Core ctor.
    private Notification() { }

    private Notification(Guid id, Guid recipientId, Guid? ideaId, string kind, string payload, DateTimeOffset createdAt)
    {
        Id = id;
        RecipientId = recipientId;
        IdeaId = ideaId;
        Kind = kind;
        Payload = payload;
        CreatedAt = createdAt;
    }

    /// <summary>Creates an unread notification.</summary>
    public static Notification Create(Guid recipientId, Guid? ideaId, string kind, string payloadJson, DateTimeOffset createdAt)
    {
        if (recipientId == Guid.Empty) throw new ArgumentException("Recipient is required.", nameof(recipientId));
        if (string.IsNullOrWhiteSpace(kind)) throw new ArgumentException("Kind is required.", nameof(kind));
        if (kind.Length > KindMaxLength) throw new ArgumentException($"Kind exceeds {KindMaxLength} characters.", nameof(kind));
        if (string.IsNullOrWhiteSpace(payloadJson)) throw new ArgumentException("Payload JSON is required.", nameof(payloadJson));
        return new Notification(Guid.NewGuid(), recipientId, ideaId, kind, payloadJson, createdAt);
    }

    /// <summary>Marks this notification as read at <paramref name="readAt"/>; idempotent.</summary>
    public void MarkRead(DateTimeOffset readAt)
    {
        ReadAt ??= readAt;
    }
}
