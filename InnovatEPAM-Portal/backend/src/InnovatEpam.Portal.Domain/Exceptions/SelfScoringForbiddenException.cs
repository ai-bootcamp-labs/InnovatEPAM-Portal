namespace InnovatEpam.Portal.Domain.Exceptions;

/// <summary>
/// Thrown when an admin tries to score their own idea (Phase 7 / FR-009).
/// Mapped to HTTP 409 with stable error code <c>SelfScoringForbidden</c>.
/// </summary>
public sealed class SelfScoringForbiddenException : Exception
{
    public const string Code = "SelfScoringForbidden";

    public SelfScoringForbiddenException()
        : base("Admins cannot score their own ideas.") { }
}
