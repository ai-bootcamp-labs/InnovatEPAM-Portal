namespace InnovatEpam.Portal.Domain.Common;

/// <summary>
/// Marker interface for entities that should have their
/// <see cref="CreatedAt"/> and <see cref="UpdatedAt"/> populated automatically
/// by the persistence layer (see <c>AuditFieldsInterceptor</c>).
/// </summary>
public interface IAuditable
{
    DateTimeOffset CreatedAt { get; set; }
    DateTimeOffset UpdatedAt { get; set; }
}
