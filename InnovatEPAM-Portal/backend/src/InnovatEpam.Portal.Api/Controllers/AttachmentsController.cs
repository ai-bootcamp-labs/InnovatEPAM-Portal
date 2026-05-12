using System.Security.Claims;
using InnovatEpam.Portal.Application.Attachments;
using InnovatEpam.Portal.Application.Ideas.Dtos;
using InnovatEpam.Portal.Domain.Identity;
using Microsoft.AspNetCore.Mvc;

namespace InnovatEpam.Portal.Api.Controllers;

/// <summary>
/// Per-idea attachment endpoints (T067). Idempotent create-or-replace at PUT,
/// streamed download at GET. Submitter or Admin only.
/// </summary>
[ApiController]
[Route("ideas/{id:guid}/attachment")] // Combined with api/v1 prefix.
public sealed class AttachmentsController : ControllerBase
{
    private const long MaxBodyBytes = 10L * 1024 * 1024;
    private readonly AttachmentService _attachments;

    public AttachmentsController(AttachmentService attachments) => _attachments = attachments;

    /// <summary>Uploads (or replaces) the attachment for the idea.</summary>
    [HttpPut]
    [RequestSizeLimit(MaxBodyBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxBodyBytes)]
    [ProducesResponseType(typeof(AttachmentSummary), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(AttachmentSummary), StatusCodes.Status200OK)]
    public async Task<ActionResult<AttachmentSummary>> Put(
        [FromRoute] Guid id,
        IFormFile file,
        CancellationToken ct)
    {
        if (file is null || file.Length == 0) return BadRequest(new { message = "No file uploaded." });

        var sub = User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(sub, out var userId)) return Unauthorized();

        await using var content = file.OpenReadStream();
        var result = await _attachments.UploadAsync(
            id, content, file.FileName, file.ContentType, userId,
            User.IsInRole(AppRole.Admin), ct);
        return result.Replaced
            ? Ok(result.Summary)
            : StatusCode(StatusCodes.Status201Created, result.Summary);
    }

    /// <summary>Streams the attachment payload back to the caller.</summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Download([FromRoute] Guid id, CancellationToken ct)
    {
        var dl = await _attachments.DownloadAsync(id, ct);
        return File(dl.Content, dl.ContentType, dl.OriginalFileName);
    }

    /// <summary>Removes the attachment for the idea (no-op if none).</summary>
    [HttpDelete]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete([FromRoute] Guid id, CancellationToken ct)
    {
        var sub = User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(sub, out var userId)) return Unauthorized();
        await _attachments.DeleteAsync(id, userId, User.IsInRole(AppRole.Admin), ct);
        return NoContent();
    }
}
