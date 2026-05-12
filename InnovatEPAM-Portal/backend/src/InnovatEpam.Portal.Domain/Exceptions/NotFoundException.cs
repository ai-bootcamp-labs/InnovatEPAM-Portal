namespace InnovatEpam.Portal.Domain.Exceptions;

/// <summary>
/// Thrown when the requested aggregate root cannot be found. Mapped to HTTP 404
/// by the global <c>DomainExceptionHandler</c>.
/// </summary>
public sealed class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message) { }
    public NotFoundException(string entityName, object key)
        : base($"{entityName} '{key}' was not found.") { }
}
