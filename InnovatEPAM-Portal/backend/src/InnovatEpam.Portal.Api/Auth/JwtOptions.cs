using System.ComponentModel.DataAnnotations;

namespace InnovatEpam.Portal.Api.Auth;

/// <summary>JWT bearer options bound from the <c>Jwt:</c> configuration section (T024, R1).</summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    [Required]
    public string Issuer { get; set; } = string.Empty;

    [Required]
    public string Audience { get; set; } = string.Empty;

    /// <summary>Symmetric HS256 signing key. Must be at least 32 characters in production.</summary>
    [Required, MinLength(32)]
    public string SigningKey { get; set; } = string.Empty;

    /// <summary>Token lifetime in minutes. Defaults to 60 (R1).</summary>
    [Range(1, 1440)]
    public int AccessTokenLifetimeMinutes { get; set; } = 60;
}
