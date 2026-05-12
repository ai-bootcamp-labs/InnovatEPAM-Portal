using InnovatEpam.Portal.Domain.Common;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace InnovatEpam.Portal.Infrastructure.Persistence.Interceptors;

/// <summary>
/// EF Core save-changes interceptor that populates
/// <see cref="IAuditable.CreatedAt"/> on inserts and
/// <see cref="IAuditable.UpdatedAt"/> on every save (T022). The API caller must
/// never set these columns directly.
/// </summary>
public sealed class AuditFieldsInterceptor : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null)
        {
            ApplyAuditFields(eventData.Context.ChangeTracker.Entries<IAuditable>());
        }
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        if (eventData.Context is not null)
        {
            ApplyAuditFields(eventData.Context.ChangeTracker.Entries<IAuditable>());
        }
        return base.SavingChanges(eventData, result);
    }

    private static void ApplyAuditFields(IEnumerable<EntityEntry<IAuditable>> entries)
    {
        var now = DateTimeOffset.UtcNow;
        foreach (var entry in entries)
        {
            switch (entry.State)
            {
                case Microsoft.EntityFrameworkCore.EntityState.Added:
                    entry.Entity.CreatedAt = now;
                    entry.Entity.UpdatedAt = now;
                    break;
                case Microsoft.EntityFrameworkCore.EntityState.Modified:
                    entry.Entity.UpdatedAt = now;
                    break;
            }
        }
    }
}
