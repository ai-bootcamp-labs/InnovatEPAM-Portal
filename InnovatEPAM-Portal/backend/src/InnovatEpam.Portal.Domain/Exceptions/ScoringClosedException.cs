namespace InnovatEpam.Portal.Domain.Exceptions;

/// <summary>
/// Thrown when an admin tries to add or update a score on an idea that has
/// already reached a terminal status (Phase 7 / FR-008). Mapped to HTTP 409
/// with stable error code <c>ScoringClosed</c>.
/// </summary>
public sealed class ScoringClosedException : Exception
{
    public const string Code = "ScoringClosed";

    public ScoringClosedException()
        : base("Scoring is closed once a decision has been recorded.") { }
}
