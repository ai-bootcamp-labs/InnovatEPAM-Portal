using InnovatEpam.Portal.Domain.Ideas;
using InnovatEpam.Portal.Domain.Identity;
using InnovatEpam.Portal.Domain.Scoring;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InnovatEpam.Portal.Infrastructure.Persistence.Configurations;

/// <summary>
/// Phase 7 — per-reviewer multi-dimension scores (FR-006..012).
/// Composite uniqueness <c>(idea_id, reviewer_id)</c> guarantees the upsert
/// semantics of <see cref="Application.Scoring.ScoringService"/>; CHECK
/// constraints mirror the in-domain <c>1..5</c> range so DB integrity holds
/// even if an out-of-band writer bypasses the application.
/// </summary>
internal sealed class IdeaScoreConfiguration : IEntityTypeConfiguration<IdeaScore>
{
    public void Configure(EntityTypeBuilder<IdeaScore> builder)
    {
        builder.ToTable("idea_scores", t =>
        {
            t.HasCheckConstraint("ck_idea_score_impact", "impact between 1 and 5");
            t.HasCheckConstraint("ck_idea_score_feasibility", "feasibility between 1 and 5");
            t.HasCheckConstraint("ck_idea_score_innovation", "innovation between 1 and 5");
            t.HasCheckConstraint("ck_idea_score_alignment", "alignment between 1 and 5");
        });

        builder.HasKey(s => s.Id);
        builder.Property(s => s.IdeaId).IsRequired();
        builder.Property(s => s.ReviewerId).IsRequired();
        builder.Property(s => s.Impact).IsRequired();
        builder.Property(s => s.Feasibility).IsRequired();
        builder.Property(s => s.Innovation).IsRequired();
        builder.Property(s => s.Alignment).IsRequired();
        builder.Property(s => s.Comment).HasMaxLength(IdeaScore.CommentMaxLength);
        builder.Property(s => s.CreatedAt).IsRequired();
        builder.Property(s => s.UpdatedAt).IsRequired();

        builder.HasIndex(s => new { s.IdeaId, s.ReviewerId })
            .IsUnique()
            .HasDatabaseName("ux_idea_score_idea_reviewer");
        builder.HasIndex(s => s.IdeaId).HasDatabaseName("ix_idea_score_idea_id");

        builder.HasOne<Idea>()
            .WithMany()
            .HasForeignKey(s => s.IdeaId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<AppUser>()
            .WithMany()
            .HasForeignKey(s => s.ReviewerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
