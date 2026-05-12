using InnovatEpam.Portal.Application.Notifications.Dtos;
using InnovatEpam.Portal.Application.Persistence;
using InnovatEpam.Portal.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace InnovatEpam.Portal.Application.Notifications;

/// <summary>
/// Reads the per-user in-portal notifications and marks them as read (T103, FR-022).
/// All queries are scoped to the caller's <paramref>RecipientId</paramref> so a
/// user can never see another user's notifications.
/// </summary>
public sealed class NotificationService
{
    /// <summary>Default page size for the notifications listing.</summary>
    public const int DefaultPageSize = 50;

    /// <summary>Maximum page size accepted by <see cref="ListAsync"/>.</summary>
    public const int MaxPageSize = 200;

    private readonly IPortalDbContext _db;

    public NotificationService(IPortalDbContext db) => _db = db;

    /// <summary>Returns the recipient's notifications, newest first.</summary>
    public async Task<IReadOnlyList<NotificationItem>> ListAsync(
        Guid recipientId,
        int? take,
        CancellationToken ct)
    {
        var pageSize = Math.Clamp(take ?? DefaultPageSize, 1, MaxPageSize);
        return await _db.Notifications.AsNoTracking()
            .Where(n => n.RecipientId == recipientId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(pageSize)
            .Select(n => new NotificationItem(n.Id, n.Kind, n.IdeaId, n.Payload, n.ReadAt, n.CreatedAt))
            .ToListAsync(ct);
    }

    /// <summary>Returns the count of unread notifications for the recipient.</summary>
    public Task<int> GetUnreadCountAsync(Guid recipientId, CancellationToken ct) =>
        _db.Notifications.AsNoTracking()
            .CountAsync(n => n.RecipientId == recipientId && n.ReadAt == null, ct);

    /// <summary>Marks the specified notification as read; idempotent.</summary>
    public async Task MarkReadAsync(Guid recipientId, Guid notificationId, CancellationToken ct)
    {
        var notification = await _db.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId, ct)
            ?? throw new NotFoundException("Notification not found.");

        if (notification.RecipientId != recipientId)
        {
            // Hide cross-tenant existence — present as not-found.
            throw new NotFoundException("Notification not found.");
        }

        if (notification.ReadAt is not null) return;
        notification.MarkRead(DateTimeOffset.UtcNow);
        await _db.SaveChangesAsync(ct);
    }
}
