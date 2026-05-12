namespace InnovatEpam.Portal.Domain.Exceptions;

/// <summary>
/// Thrown when an inbound request fails domain or DTO validation. Mapped to
/// HTTP 400 (with field-level errors) by the global <c>DomainExceptionHandler</c>.
/// </summary>
public sealed class DomainValidationException : Exception
{
    public IReadOnlyDictionary<string, string[]> Errors { get; }

    public DomainValidationException(string message, IReadOnlyDictionary<string, string[]>? errors = null)
        : base(message)
    {
        Errors = errors ?? new Dictionary<string, string[]>();
    }
}
