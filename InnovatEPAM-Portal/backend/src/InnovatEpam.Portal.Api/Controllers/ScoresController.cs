using System.Security.Claims;
using InnovatEpam.Portal.Application.Scoring;
using InnovatEpam.Portal.Application.Scoring.Dtos;
using InnovatEpam.Portal.Domain.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InnovatEpam.Portal.Api.Controllers;

/// <summary>
/// Phase 7 — reviewer scoring endpoints (FR-006, FR-008).
/// Admins POST a score per dimension; second POST from the same reviewer
/// upserts the row in place. Returns the refreshed aggregate so the client
/// can render new averages without a follow-up GET.
/// </summary>
[ApiController]
[Route("ideas/{ideaId:guid}/scores")]
[Authorize(Roles = AppRole.Admin)]
public sealed class ScoresController : ControllerBase
{
    private readonly ScoringService _scoring;

    public ScoresController(ScoringService scoring) => _scoring = scoring;

    [HttpPost]
    [ProducesResponseType(typeof(IdeaScoreAggregateDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(IdeaScoreAggregateDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<IdeaScoreAggregateDto>> Submit(
        [FromRoute] Guid ideaId,
        [FromBody] SubmitScoreRequest request,
        CancellationToken ct)
    {
        var sub = User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(sub, out var reviewerId)) return Unauthorized();

        var result = await _scoring.UpsertAsync(ideaId, reviewerId, request, ct);
        return result.Created
            ? StatusCode(StatusCodes.Status201Created, result.Aggregate)
            : Ok(result.Aggregate);
    }
}
