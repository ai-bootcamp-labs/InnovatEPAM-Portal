namespace InnovatEpam.Portal.Domain.Categories;

/// <summary>
/// Closed list of innovation categories (FR-009). Seeded by the initial
/// migration with <c>process</c>, <c>product</c>, <c>technology</c>,
/// <c>people</c>, <c>other</c>. See data-model §3.
/// </summary>
public sealed class Category
{
    private Category() { }

    private Category(Guid id, string code, string name, int sortOrder)
    {
        Id = id;
        Code = code;
        Name = name;
        SortOrder = sortOrder;
        IsActive = true;
    }

    public Guid Id { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;
    public int SortOrder { get; private set; }

    /// <summary>Factory used by tests and seed data.</summary>
    public static Category Create(Guid id, string code, string name, int sortOrder = 0)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return new Category(id, code, name, sortOrder);
    }
}
