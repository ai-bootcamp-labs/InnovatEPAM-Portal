namespace InnovatEpam.Portal.Domain.Exceptions;

/// <summary>
/// Thrown when an operation violates a domain invariant or concurrency
/// constraint. Mapped to HTTP 409 by the global <c>DomainExceptionHandler</c>.
/// </summary>
public sealed class ConflictException : Exception
{
    public ConflictException(string message) : base(message) { }
}
