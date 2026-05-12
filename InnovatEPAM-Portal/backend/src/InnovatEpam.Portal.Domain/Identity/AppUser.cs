using Microsoft.AspNetCore.Identity;

namespace InnovatEpam.Portal.Domain.Identity;

/// <summary>
/// Application user. Single-role-per-user via <see cref="RoleId"/> (FR-004).
/// Email uniqueness is enforced case-insensitively at the database level
/// (PostgreSQL <c>citext</c>); see data-model §1.
/// </summary>
public class AppUser : IdentityUser<Guid>
{
    public string DisplayName { get; set; } = string.Empty;
    public Guid RoleId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }

    public AppRole? Role { get; set; }
}
