using InnovatEpam.Portal.Application.Persistence;
using InnovatEpam.Portal.Domain.Categories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InnovatEpam.Portal.Api.Controllers;

/// <summary>Closed-list category catalogue (T068, FR-009).</summary>
[ApiController]
[Route("categories")] // Combined with api/v1 prefix.
public sealed class CategoriesController : ControllerBase
{
    private readonly IPortalDbContext _db;

    public CategoriesController(IPortalDbContext db) => _db = db;

    /// <summary>Returns the active categories ordered by sort_order.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<CategoryDto>), StatusCodes.Status200OK)]
    public async Task<IReadOnlyList<CategoryDto>> List(CancellationToken ct) =>
        await _db.Categories
            .AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.SortOrder).ThenBy(c => c.Name)
            .Select(c => new CategoryDto(c.Id, c.Code, c.Name, c.SortOrder))
            .ToListAsync(ct);

    public sealed record CategoryDto(Guid Id, string Code, string Name, int SortOrder);
}
