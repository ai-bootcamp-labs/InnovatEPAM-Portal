namespace InnovatEpam.Portal.Domain.Exceptions;

/// <summary>
/// Thrown when an authenticated principal lacks permission for an operation.
/// Mapped to HTTP 403 by the global <c>DomainExceptionHandler</c>.
/// </summary>
public sealed class ForbiddenException : Exception
{
    public ForbiddenException(string message) : base(message) { }
}
