using InnovatEpam.Portal.Domain.Identity;
using InnovatEpam.Portal.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace InnovatEpam.Portal.Infrastructure.Seeding;

/// <summary>
/// T107 — seeds an Admin user from configuration when no Admin exists.
///
/// Reads <c>Seed:AdminEmail</c> and <c>Seed:AdminPassword</c>; if either is
/// absent the seeder is a no-op (callers should still log a warning so the
/// operator notices). The check is idempotent: re-running on a fresh container
/// will not create duplicate admins.
/// </summary>
public static class AdminUserSeeder
{
    private const string SectionName = "Seed";

    /// <summary>Ensures at least one Admin user exists. Safe to call on every startup.</summary>
    public static async Task SeedAsync(IServiceProvider services, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(services);

        var configuration = services.GetRequiredService<IConfiguration>();
        var logger = services.GetRequiredService<ILogger<PortalDbContext>>();

        var email = configuration[$"{SectionName}:AdminEmail"];
        var password = configuration[$"{SectionName}:AdminPassword"];
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            logger.LogWarning("seed.admin.skipped — Seed:AdminEmail or Seed:AdminPassword is not configured.");
            return;
        }

        var userManager = services.GetRequiredService<UserManager<AppUser>>();
        var roleManager = services.GetRequiredService<RoleManager<AppRole>>();

        var adminRole = await roleManager.FindByNameAsync(AppRole.Admin)
            ?? throw new InvalidOperationException("Admin role missing — initial migration was not applied.");

        var existingAdmin = await userManager.Users
            .Where(u => u.RoleId == adminRole.Id)
            .Select(u => u.Id)
            .FirstOrDefaultAsync(ct);
        if (existingAdmin != Guid.Empty)
        {
            logger.LogInformation("seed.admin.skipped — Admin already exists.");
            return;
        }

        var existingByEmail = await userManager.FindByEmailAsync(email);
        if (existingByEmail is not null)
        {
            logger.LogWarning("seed.admin.skipped — user {Email} exists but is not in the Admin role.", email);
            return;
        }

        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            DisplayName = "Portal Administrator",
            RoleId = adminRole.Id,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        var createResult = await userManager.CreateAsync(user, password);
        if (!createResult.Succeeded)
        {
            var detail = string.Join("; ", createResult.Errors.Select(e => $"{e.Code}: {e.Description}"));
            logger.LogError("seed.admin.failed {Detail}", detail);
            return;
        }

        var roleResult = await userManager.AddToRoleAsync(user, AppRole.Admin);
        if (!roleResult.Succeeded)
        {
            var detail = string.Join("; ", roleResult.Errors.Select(e => $"{e.Code}: {e.Description}"));
            logger.LogError("seed.admin.role_assignment_failed {Detail}", detail);
            return;
        }

        logger.LogInformation("seed.admin.created {UserId} {Email}", user.Id, email);
    }
}
