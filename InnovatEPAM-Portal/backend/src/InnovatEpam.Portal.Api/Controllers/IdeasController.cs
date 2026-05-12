using System.Security.Claims;
using InnovatEpam.Portal.Application.Ideas;
using InnovatEpam.Portal.Application.Ideas.Dtos;
using InnovatEpam.Portal.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace InnovatEpam.Portal.Api.Controllers;

/// <summary>Idea CRUD + listing endpoints (T066).</summary>
[ApiController]
[Route("ideas")] // Combined with api/v1 prefix.
public sealed class IdeasController : ControllerBase
{
    private readonly IdeaService _ideas;

    public IdeasController(IdeaService ideas) => _ideas = ideas;

    /// <summary>Returns a filtered, paginated list of ideas.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedIdeas), StatusCodes.Status200OK)]
    public Task<PagedIdeas> List(
        [FromQuery] IdeaStatus? status,
        [FromQuery(Name = "categoryCode")] string? categoryCode,
        [FromQuery] Guid? submitterId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? sort = null,
        CancellationToken ct = default) =>
        _ideas.ListAsync(status, categoryCode, submitterId, page, pageSize, ct, sort);

    /// <summary>Returns the full detail projection for a single idea.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(IdeaDetail), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IdeaDetail> Get([FromRoute] Guid id, CancellationToken ct) => _ideas.GetByIdAsync(id, ct);

    /// <summary>Submits a new idea on behalf of the authenticated user.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(IdeaDetail), StatusCodes.Status201Created)]
    public async Task<ActionResult<IdeaDetail>> Create([FromBody] CreateIdeaRequest request, CancellationToken ct)
    {
        var sub = User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(sub, out var userId)) return Unauthorized();
        var idea = await _ideas.CreateAsync(request, userId, ct);
        return CreatedAtAction(nameof(Get), new { id = idea.Id }, idea);
    }
}
