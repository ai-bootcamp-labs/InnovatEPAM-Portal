using InnovatEpam.Portal.Application.Auth.Dtos;
using InnovatEpam.Portal.Domain.Exceptions;
using InnovatEpam.Portal.Domain.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace InnovatEpam.Portal.Application.Auth;

/// <summary>
/// Coordinates user registration, login and "who am I" lookups (T062).
/// Wraps ASP.NET Core Identity's <see cref="UserManager{TUser}"/> and the
/// JWT issuer; never logs raw passwords (R7, FR-025).
/// </summary>
public sealed class AuthService
{
    private readonly UserManager<AppUser> _userManager;
    private readonly RoleManager<AppRole> _roleManager;
    private readonly IJwtTokenIssuer _tokenIssuer;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        UserManager<AppUser> userManager,
        RoleManager<AppRole> roleManager,
        IJwtTokenIssuer tokenIssuer,
        ILogger<AuthService> logger)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _tokenIssuer = tokenIssuer;
        _logger = logger;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct)
    {
        var existing = await _userManager.FindByEmailAsync(request.Email);
        if (existing is not null)
        {
            throw new ConflictException("Email is already registered.");
        }

        var role = await _roleManager.FindByNameAsync(AppRole.Submitter)
            ?? throw new InvalidOperationException("Submitter role missing — migration was not applied.");

        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            UserName = request.Email,
            Email = request.Email,
            DisplayName = request.DisplayName.Trim(),
            RoleId = role.Id,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        var createResult = await _userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
        {
            ThrowFromIdentityErrors(createResult);
        }
        var roleResult = await _userManager.AddToRoleAsync(user, AppRole.Submitter);
        if (!roleResult.Succeeded)
        {
            ThrowFromIdentityErrors(roleResult);
        }

        _logger.LogInformation("auth.register.success {UserId} {Email}", user.Id, user.Email);
        return await IssueAsync(user, AppRole.Submitter);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null || !user.IsActive)
        {
            _logger.LogWarning("auth.login.failure {Email} {Reason}", request.Email, "unknown_or_inactive");
            throw new ForbiddenException("Invalid credentials.");
        }
        var ok = await _userManager.CheckPasswordAsync(user, request.Password);
        if (!ok)
        {
            _logger.LogWarning("auth.login.failure {Email} {Reason}", request.Email, "bad_password");
            throw new ForbiddenException("Invalid credentials.");
        }

        var roleName = await GetPrimaryRoleNameAsync(user);
        _logger.LogInformation("auth.login.success {UserId} {Email}", user.Id, user.Email);
        return await IssueAsync(user, roleName);
    }

    public async Task<UserSummary> GetMeAsync(Guid userId, CancellationToken ct)
    {
        var user = await _userManager.Users
            .Where(u => u.Id == userId)
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("User not found.");
        var roleName = await GetPrimaryRoleNameAsync(user);
        return new UserSummary(user.Id, user.Email ?? string.Empty, user.DisplayName, roleName);
    }

    private Task<AuthResponse> IssueAsync(AppUser user, string roleName)
    {
        var (token, expiresAt) = _tokenIssuer.CreateAccessToken(user, roleName);
        return Task.FromResult(new AuthResponse(token, expiresAt,
            new UserSummary(user.Id, user.Email ?? string.Empty, user.DisplayName, roleName)));
    }

    private async Task<string> GetPrimaryRoleNameAsync(AppUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);
        return roles.FirstOrDefault() ?? AppRole.Submitter;
    }

    private static void ThrowFromIdentityErrors(IdentityResult result)
    {
        var errors = result.Errors
            .GroupBy(e => string.IsNullOrEmpty(e.Code) ? "Identity" : e.Code)
            .ToDictionary(g => g.Key, g => g.Select(e => e.Description).ToArray());
        throw new DomainValidationException("Identity operation failed.", errors);
    }
}
