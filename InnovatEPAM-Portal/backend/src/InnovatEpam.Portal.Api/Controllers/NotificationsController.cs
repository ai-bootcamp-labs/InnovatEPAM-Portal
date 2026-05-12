using System.Security.Claims;
using InnovatEpam.Portal.Application.Notifications;
using InnovatEpam.Portal.Application.Notifications.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace InnovatEpam.Portal.Api.Controllers;

/// <summary>
/// In-portal notifications endpoints (T103, FR-022). Authenticated by the
/// global fallback policy; results are scoped to the calling user.
/// </summary>
[ApiController]
[Route("notifications")] // Combined with api/v1 prefix.
public sealed class NotificationsController : ControllerBase
{
    private readonly NotificationService _notifications;

    public NotificationsController(NotificationService notifications) => _notifications = notifications;

    /// <summary>Returns the most recent notifications for the caller (newest first).</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<NotificationItem>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<NotificationItem>>> List(
        [FromQuery] int? take,
        CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        return Ok(await _notifications.ListAsync(userId, take, ct));
    }

    /// <summary>Returns the count of unread notifications for the caller.</summary>
    [HttpGet("unread-count")]
    [ProducesResponseType(typeof(UnreadCountResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<UnreadCountResponse>> UnreadCount(CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var count = await _notifications.GetUnreadCountAsync(userId, ct);
        return Ok(new UnreadCountResponse(count));
    }

    /// <summary>Marks the notification as read. Idempotent; 404 when not owned by the caller.</summary>
    [HttpPost("{id:guid}/read")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkRead([FromRoute] Guid id, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        await _notifications.MarkReadAsync(userId, id, ct);
        return NoContent();
    }

    private bool TryGetUserId(out Guid userId)
    {
        var sub = User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(sub, out userId);
    }
}
