using InnovatEpam.Portal.Domain.Ideas;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InnovatEpam.Portal.Infrastructure.Persistence.Configurations;

internal sealed class IdeaConfiguration : IEntityTypeConfiguration<Idea>
{
    public void Configure(EntityTypeBuilder<Idea> builder)
    {
        builder.ToTable("ideas", t =>
        {
            t.HasCheckConstraint("ck_idea_title_length",
                $"char_length(title) between {Idea.TitleMinLength} and {Idea.TitleMaxLength}");
            t.HasCheckConstraint("ck_idea_description_length",
                $"char_length(description) between {Idea.DescriptionMinLength} and {Idea.DescriptionMaxLength}");
            t.HasCheckConstraint("ck_idea_status", "status between 1 and 4");
        });

        builder.HasKey(i => i.Id);
        builder.Property(i => i.Title).HasMaxLength(Idea.TitleMaxLength).IsRequired();
        builder.Property(i => i.Description).HasMaxLength(Idea.DescriptionMaxLength).IsRequired();
        builder.Property(i => i.CategoryId).IsRequired();
        builder.Property(i => i.SubmitterId).IsRequired();
        builder.Property(i => i.Status).HasConversion<int>().HasDefaultValue(Domain.Enums.IdeaStatus.Submitted).IsRequired();
        builder.Property(i => i.CreatedAt).IsRequired();
        builder.Property(i => i.UpdatedAt).IsRequired();

        // PostgreSQL xmin system column → optimistic concurrency.
        builder.Property(i => i.RowVersion)
            .IsRowVersion()
            .HasColumnName("xmin")
            .HasColumnType("xid");

        builder.HasOne<Domain.Categories.Category>()
            .WithMany()
            .HasForeignKey(i => i.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Domain.Identity.AppUser>()
            .WithMany()
            .HasForeignKey(i => i.SubmitterId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(i => new { i.Status, i.CreatedAt }).HasDatabaseName("ix_idea_status_created_at");
        builder.HasIndex(i => i.CategoryId).HasDatabaseName("ix_idea_category_id");
        builder.HasIndex(i => i.SubmitterId).HasDatabaseName("ix_idea_submitter_id");
        builder.HasIndex(i => i.CreatedAt).HasDatabaseName("ix_idea_created_at");
    }
}
