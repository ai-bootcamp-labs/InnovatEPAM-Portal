namespace InnovatEpam.Portal.Domain.Categories;

/// <summary>
/// Closed list of innovation categories (FR-009). Seeded by the initial
/// migration with <c>process</c>, <c>product</c>, <c>technology</c>,
/// <c>people</c>, <c>other</c>. See data-model §3.
/// </summary>
/// <remarks>
/// Phase 2 stub — Phase 3 (T054) replaces with a domain-driven aggregate.
/// </remarks>
public class Category
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
}
