using FluentAssertions;
using InnovatEpam.Portal.Application.Decisions;
using InnovatEpam.Portal.Application.Decisions.Dtos;
using InnovatEpam.Portal.Application.Notifications;
using InnovatEpam.Portal.Application.Persistence;
using InnovatEpam.Portal.Domain.Categories;
using InnovatEpam.Portal.Domain.Enums;
using InnovatEpam.Portal.Domain.Ideas;
using InnovatEpam.Portal.Domain.Identity;
using InnovatEpam.Portal.Domain.Notifications;
using InnovatEpam.Portal.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace InnovatEpam.Portal.IntegrationTests.Notifications;

/// <summary>
/// T104 — verifies that a decision creates exactly one unread notification for
/// the submitter, that the mark-read endpoint sets <c>read_at</c>, and that the
/// unread count drops accordingly.
///
/// Requires Docker on the host (Testcontainers spins up a Postgres 15 container).
/// The fixture is local to this file so it is independent of the (still-pending)
/// shared <c>DatabaseFixture</c> from T036.
/// </summary>
public sealed class NotificationsTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:15-alpine")
        .WithDatabase("innovatepam_test")
        .WithUsername("test")
        .WithPassword("test")
        .Build();

    private ServiceProvider _services = null!;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<PortalDbContext>(o =>
            o.UseNpgsql(_postgres.GetConnectionString())
             .UseSnakeCaseNamingConvention());
        services.AddScoped<IPortalDbContext>(sp => sp.GetRequiredService<PortalDbContext>());
        services.AddScoped<InnovatEpam.Portal.Application.Ideas.IdeaService>();
        services.AddScoped<DecisionService>();
        services.AddScoped<NotificationService>();

        _services = services.BuildServiceProvider();

        await using var scope = _services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<PortalDbContext>();
        await db.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await _services.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    [Fact(Skip = "Requires Docker; enable when CI runs Testcontainers (T036).")]
    public async Task Decision_creates_one_unread_notification_for_submitter_and_mark_read_clears_count()
    {
        await using var scope = _services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<PortalDbContext>();
        var decisions = scope.ServiceProvider.GetRequiredService<DecisionService>();
        var notifications = scope.ServiceProvider.GetRequiredService<NotificationService>();

        var category = await db.Categories.FirstAsync();
        var submitter = new AppUser { Id = Guid.NewGuid(), UserName = "submitter@test.local", Email = "submitter@test.local", DisplayName = "Submitter One", CreatedAt = DateTimeOffset.UtcNow };
        var admin = new AppUser { Id = Guid.NewGuid(), UserName = "admin@test.local", Email = "admin@test.local", DisplayName = "Admin One", CreatedAt = DateTimeOffset.UtcNow };
        db.Users.Add(submitter);
        db.Users.Add(admin);
        await db.SaveChangesAsync();

        var idea = Idea.Create("Phase 6 notifications smoke", "Body of the idea.", category.Id, submitter.Id);
        db.Ideas.Add(idea);
        await db.SaveChangesAsync();

        await decisions.RecordAsync(idea.Id, new CreateDecisionRequest(DecisionAction.MoveToUnderReview, null), admin.Id, default);

        var unreadBefore = await notifications.GetUnreadCountAsync(submitter.Id, default);
        unreadBefore.Should().Be(1);

        var list = await notifications.ListAsync(submitter.Id, take: null, default);
        list.Should().HaveCount(1);
        list[0].Kind.Should().Be(Notification.KindIdeaStatusChanged);
        list[0].ReadAt.Should().BeNull();

        await notifications.MarkReadAsync(submitter.Id, list[0].Id, default);

        var unreadAfter = await notifications.GetUnreadCountAsync(submitter.Id, default);
        unreadAfter.Should().Be(0);

        var refreshed = await db.Notifications.AsNoTracking().FirstAsync(n => n.Id == list[0].Id);
        refreshed.ReadAt.Should().NotBeNull();
    }
}
