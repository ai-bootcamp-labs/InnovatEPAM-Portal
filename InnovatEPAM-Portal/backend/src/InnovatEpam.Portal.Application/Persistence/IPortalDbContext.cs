using InnovatEpam.Portal.Domain.Attachments;
using InnovatEpam.Portal.Domain.Categories;
using InnovatEpam.Portal.Domain.Decisions;
using InnovatEpam.Portal.Domain.Ideas;
using InnovatEpam.Portal.Domain.Identity;
using InnovatEpam.Portal.Domain.Notifications;
using InnovatEpam.Portal.Domain.Scoring;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace InnovatEpam.Portal.Application.Persistence;

/// <summary>
/// Application-layer abstraction over the EF Core <c>PortalDbContext</c>.
/// Lets services live in the Application project without leaking Infrastructure types.
/// </summary>
public interface IPortalDbContext
{
    DbSet<AppUser> Users { get; }
    DbSet<AppRole> Roles { get; }
    DbSet<Category> Categories { get; }
    DbSet<Idea> Ideas { get; }
    DbSet<Attachment> Attachments { get; }
    DbSet<Decision> Decisions { get; }
    DbSet<IdeaStatusHistory> IdeaStatusHistory { get; }
    DbSet<Notification> Notifications { get; }
    DbSet<IdeaScore> IdeaScores { get; }
    DatabaseFacade Database { get; }
    Task<int> SaveChangesAsync(CancellationToken ct);
}
