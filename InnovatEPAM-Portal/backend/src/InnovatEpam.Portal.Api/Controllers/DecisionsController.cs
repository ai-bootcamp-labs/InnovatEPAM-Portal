using System.Security.Claims;
using InnovatEpam.Portal.Application.Decisions;
using InnovatEpam.Portal.Application.Decisions.Dtos;
using InnovatEpam.Portal.Application.Ideas.Dtos;
using InnovatEpam.Portal.Domain.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InnovatEpam.Portal.Api.Controllers;

/// <summary>
/// Admin decision endpoints (T086) and the read-only status timeline (T087).
/// History is readable by any authenticated user; recording a decision is Admin-only.
/// </summary>
[ApiController]
[Route("ideas/{id:guid}")] // Combined with api/v1 prefix.
public sealed class DecisionsController : ControllerBase
{
    private readonly DecisionService _decisions;

    public DecisionsController(DecisionService decisions) => _decisions = decisions;

    /// <summary>Records a new decision against the idea.</summary>
    [HttpPost("decisions")]
    [Authorize(Roles = AppRole.Admin)]
    [ProducesResponseType(typeof(IdeaDetail), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<IdeaDetail>> Record(
        [FromRoute] Guid id,
        [FromBody] CreateDecisionRequest request,
        CancellationToken ct)
    {
        var sub = User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(sub, out var adminId)) return Unauthorized();
        return Ok(await _decisions.RecordAsync(id, request, adminId, ct));
    }

    /// <summary>Returns the chronological status history for the idea (FR-021).</summary>
    [HttpGet("history")]
    [ProducesResponseType(typeof(IReadOnlyList<StatusHistoryEntry>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IReadOnlyList<StatusHistoryEntry>> History([FromRoute] Guid id, CancellationToken ct) =>
        _decisions.GetHistoryAsync(id, ct);
}
