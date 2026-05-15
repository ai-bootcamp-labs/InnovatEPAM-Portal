using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace InnovatEpam.Portal.Application.Auth;

/// <summary>
/// Deterministic, salted alias generator used by Phase 6 (Blind Review) and
/// Phase 7 (Reviewer aliases). Aliases are computed via HMAC-SHA256 over the
/// input parts, truncated and base32-encoded for a short, human-readable token.
/// </summary>
/// <remarks>
/// <para>
/// The salt is read from the <c>BlindReview:AliasSalt</c> configuration value.
/// Production deployments MUST override this via user-secrets / env vars; the
/// development default is non-secret and rotated independently of code.
/// </para>
/// <para>
/// Aliases are derived from <c>IdeaId</c> (per-submitter) and
/// <c>(IdeaId, ReviewerId)</c> (per-reviewer), so two ideas authored by the
/// same submitter resolve to different aliases (FR-002).
/// </para>
/// </remarks>
public interface IAliasService
{
    /// <summary>Alias for the submitter of <paramref name="ideaId"/>.</summary>
    string SubmitterAlias(Guid ideaId);

    /// <summary>Alias for a reviewer who scored <paramref name="ideaId"/>.</summary>
    string ReviewerAlias(Guid ideaId, Guid reviewerId);
}

/// <inheritdoc cref="IAliasService"/>
public sealed class AliasService : IAliasService
{
    public const string ConfigKey = "BlindReview:AliasSalt";
    private const string DefaultDevSalt = "innovatepam-dev-only-alias-salt-do-not-use-in-prod";
    private const int TokenLength = 4;
    private static readonly char[] Base32Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789".ToCharArray();

    private readonly byte[] _saltBytes;

    public AliasService(IConfiguration configuration, ILogger<AliasService>? logger = null)
    {
        var raw = configuration[ConfigKey];
        if (string.IsNullOrWhiteSpace(raw))
        {
            logger?.LogWarning(
                "BlindReview:AliasSalt not configured; using a built-in development salt. " +
                "This MUST be overridden in production.");
            raw = DefaultDevSalt;
        }
        _saltBytes = Encoding.UTF8.GetBytes(raw);
    }

    public string SubmitterAlias(Guid ideaId)
        => $"Submitter #{ComputeToken("S", ideaId.ToString("N"))}";

    public string ReviewerAlias(Guid ideaId, Guid reviewerId)
        => $"Reviewer #{ComputeToken("R", ideaId.ToString("N"), reviewerId.ToString("N"))}";

    private string ComputeToken(params string[] parts)
    {
        var payload = string.Join("|", parts);
        using var hmac = new HMACSHA256(_saltBytes);
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return EncodeBase32(hash, TokenLength);
    }

    private static string EncodeBase32(ReadOnlySpan<byte> bytes, int chars)
    {
        // Pack the top 5 * chars bits of the hash into the base32 alphabet.
        var sb = new StringBuilder(chars);
        for (var i = 0; i < chars; i++)
        {
            var bitIndex = i * 5;
            var byteIndex = bitIndex / 8;
            var bitOffset = bitIndex % 8;
            int b1 = bytes[byteIndex];
            int b2 = byteIndex + 1 < bytes.Length ? bytes[byteIndex + 1] : 0;
            var combined = ((b1 << 8) | b2) >> (16 - bitOffset - 5);
            sb.Append(Base32Alphabet[combined & 0x1F]);
        }
        return sb.ToString();
    }
}
