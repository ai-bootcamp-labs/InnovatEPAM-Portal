using InnovatEpam.Portal.Domain.Attachments;
using InnovatEpam.Portal.Domain.Categories;
using InnovatEpam.Portal.Domain.Decisions;
using InnovatEpam.Portal.Domain.Identity;
using InnovatEpam.Portal.Domain.Ideas;
using InnovatEpam.Portal.Domain.Notifications;
using InnovatEpam.Portal.Domain.Outbox;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace InnovatEpam.Portal.Infrastructure.Persistence;

/// <summary>
/// Primary EF Core context (T021). All tables live in the <c>portal</c> schema
/// and use snake_case column names via <c>UseSnakeCaseNamingConvention()</c>.
/// </summary>
public class PortalDbContext : IdentityDbContext<AppUser, AppRole, Guid>
{
    public const string SchemaName = "portal";

    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Idea> Ideas => Set<Idea>();
    public DbSet<Attachment> Attachments => Set<Attachment>();
    public DbSet<Decision> Decisions => Set<Decision>();
    public DbSet<IdeaStatusHistory> IdeaStatusHistory => Set<IdeaStatusHistory>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    public PortalDbContext(DbContextOptions<PortalDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.HasDefaultSchema(SchemaName);
        base.OnModelCreating(builder);

        // Entity configurations (CHECK constraints, indexes, FK delete behaviour,
        // xmin row-version) are added in T058 (Phase 3 / US1).
        builder.ApplyConfigurationsFromAssembly(typeof(PortalDbContext).Assembly);
    }
}
