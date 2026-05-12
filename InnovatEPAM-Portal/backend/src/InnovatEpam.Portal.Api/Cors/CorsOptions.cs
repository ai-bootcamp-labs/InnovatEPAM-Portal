namespace InnovatEpam.Portal.Api.Cors;

/// <summary>CORS allow-list bound from the <c>Cors:</c> configuration section (T029).</summary>
public sealed class CorsOptions
{
    public const string SectionName = "Cors";
    public const string DefaultPolicyName = "DefaultCorsPolicy";

    public string[] AllowedOrigins { get; set; } = Array.Empty<string>();
}
