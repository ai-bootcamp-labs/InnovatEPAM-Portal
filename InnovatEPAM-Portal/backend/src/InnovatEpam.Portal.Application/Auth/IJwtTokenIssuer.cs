using InnovatEpam.Portal.Domain.Identity;

namespace InnovatEpam.Portal.Application.Auth;

/// <summary>Issues JWT bearer tokens for authenticated users (R1).</summary>
public interface IJwtTokenIssuer
{
    /// <summary>Creates a signed access token for <paramref name="user"/> with role <paramref name="roleName"/>.</summary>
    (string AccessToken, DateTimeOffset ExpiresAt) CreateAccessToken(AppUser user, string roleName);
}
