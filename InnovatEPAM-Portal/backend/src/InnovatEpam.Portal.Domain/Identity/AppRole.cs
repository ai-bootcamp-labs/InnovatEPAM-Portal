using Microsoft.AspNetCore.Identity;

namespace InnovatEpam.Portal.Domain.Identity;

/// <summary>
/// Application role. Closed list seeded by the initial migration:
/// <c>Submitter</c> and <c>Admin</c> (FR-003, FR-004).
/// </summary>
public class AppRole : IdentityRole<Guid>
{
    public const string Submitter = "Submitter";
    public const string Admin = "Admin";

    public AppRole() { }
    public AppRole(string roleName) : base(roleName) { }
}
