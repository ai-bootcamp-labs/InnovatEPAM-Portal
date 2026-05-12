using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using InnovatEpam.Portal.Api.Auth;
using InnovatEpam.Portal.Application.Auth;
using InnovatEpam.Portal.Domain.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace InnovatEpam.Portal.Api.Auth;

/// <summary>
/// HS256 JWT issuer (R1, T024). Token lifetime is read from <see cref="JwtOptions.AccessTokenLifetimeMinutes"/>.
/// </summary>
internal sealed class JwtTokenIssuer : IJwtTokenIssuer
{
    private readonly JwtOptions _options;
    private readonly SigningCredentials _credentials;

    public JwtTokenIssuer(IOptions<JwtOptions> options)
    {
        _options = options.Value;
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));
        _credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
    }

    public (string AccessToken, DateTimeOffset ExpiresAt) CreateAccessToken(AppUser user, string roleName)
    {
        var now = DateTimeOffset.UtcNow;
        var expiresAt = now.AddMinutes(_options.AccessTokenLifetimeMinutes);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("displayName", user.DisplayName),
            new(ClaimTypes.Role, roleName),
        };
        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: now.UtcDateTime,
            expires: expiresAt.UtcDateTime,
            signingCredentials: _credentials);
        var handler = new JwtSecurityTokenHandler();
        return (handler.WriteToken(token), expiresAt);
    }
}
