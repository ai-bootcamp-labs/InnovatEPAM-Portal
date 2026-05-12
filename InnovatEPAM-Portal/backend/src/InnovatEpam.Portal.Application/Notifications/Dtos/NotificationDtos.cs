namespace InnovatEpam.Portal.Application.Notifications.Dtos;

/// <summary>One row of the per-user notifications list (T103, FR-022).</summary>
public sealed record NotificationItem(
    Guid Id,
    string Kind,
    Guid? IdeaId,
    string Payload,
    DateTimeOffset? ReadAt,
    DateTimeOffset CreatedAt);

/// <summary>Response body for the unread-count poll (T103, T105).</summary>
public sealed record UnreadCountResponse(int Count);
