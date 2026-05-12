using InnovatEpam.Portal.Domain.Decisions;
using InnovatEpam.Portal.Domain.Ideas;
using InnovatEpam.Portal.Domain.Notifications;
using InnovatEpam.Portal.Domain.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InnovatEpam.Portal.Infrastructure.Persistence.Configurations;

internal sealed class DecisionConfiguration : IEntityTypeConfiguration<Decision>
{
    public void Configure(EntityTypeBuilder<Decision> builder)
    {
        builder.ToTable("decisions", t =>
        {
            t.HasCheckConstraint("ck_decision_action", "action between 1 and 3");
            t.HasCheckConstraint("ck_decision_comment_required_for_terminal",
                "(action = 1) or (action in (2,3) and comment is not null and char_length(comment) between 1 and 2000)");
        });

        builder.HasKey(d => d.Id);
        builder.Property(d => d.IdeaId).IsRequired();
        builder.Property(d => d.Action).HasConversion<int>().IsRequired();
        builder.Property(d => d.Comment).HasMaxLength(Decision.CommentMaxLength);
        builder.Property(d => d.DecidedById).IsRequired();
        builder.Property(d => d.DecidedAt).IsRequired();

        builder.HasIndex(d => new { d.IdeaId, d.DecidedAt })
            .HasDatabaseName("ix_decision_idea_id_decided_at");
        builder.HasIndex(d => d.DecidedById)
            .HasDatabaseName("ix_decision_decided_by_id");

        builder.HasOne<Idea>()
            .WithMany()
            .HasForeignKey(d => d.IdeaId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class IdeaStatusHistoryConfiguration : IEntityTypeConfiguration<IdeaStatusHistory>
{
    public void Configure(EntityTypeBuilder<IdeaStatusHistory> builder)
    {
        builder.ToTable("idea_status_history", t =>
        {
            t.HasCheckConstraint("ck_status_history_to_status", "to_status between 1 and 4");
            t.HasCheckConstraint("ck_status_history_from_status",
                "from_status is null or from_status between 1 and 4");
            t.HasCheckConstraint("ck_status_history_transition_valid",
                "(from_status is null and to_status = 1) " +
                "or (from_status = 1 and to_status in (2,3,4)) " +
                "or (from_status = 2 and to_status in (3,4))");
        });

        builder.HasKey(h => h.Id);
        builder.Property(h => h.IdeaId).IsRequired();
        builder.Property(h => h.FromStatus).HasConversion<int?>();
        builder.Property(h => h.ToStatus).HasConversion<int>().IsRequired();
        builder.Property(h => h.ActorId).IsRequired();
        builder.Property(h => h.Comment).HasMaxLength(2000);
        builder.Property(h => h.OccurredAt).IsRequired();

        builder.HasIndex(h => new { h.IdeaId, h.OccurredAt })
            .HasDatabaseName("ix_status_history_idea_id_occurred_at");

        builder.HasOne<Idea>()
            .WithMany()
            .HasForeignKey(h => h.IdeaId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Decision>()
            .WithMany()
            .HasForeignKey(h => h.DecisionId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

internal sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("notifications");
        builder.HasKey(n => n.Id);
        builder.Property(n => n.RecipientId).IsRequired();
        builder.Property(n => n.Kind).HasMaxLength(64).IsRequired();
        builder.Property(n => n.Payload).HasColumnType("jsonb").IsRequired();
        builder.Property(n => n.CreatedAt).IsRequired();

        builder.HasIndex(n => n.RecipientId)
            .HasFilter("read_at is null")
            .HasDatabaseName("ix_notification_recipient_unread");
        builder.HasIndex(n => n.IdeaId).HasDatabaseName("ix_notification_idea_id");

        builder.HasOne<Idea>()
            .WithMany()
            .HasForeignKey(n => n.IdeaId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("outbox_messages");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.OccurredAt).IsRequired();
        builder.Property(m => m.Type).HasMaxLength(256).IsRequired();
        builder.Property(m => m.Payload).HasColumnType("jsonb").IsRequired();

        builder.HasIndex(m => m.OccurredAt)
            .HasFilter("processed_at is null")
            .HasDatabaseName("ix_outbox_unprocessed");
    }
}
