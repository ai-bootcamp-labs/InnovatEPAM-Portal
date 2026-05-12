using InnovatEpam.Portal.Domain.Attachments;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InnovatEpam.Portal.Infrastructure.Persistence.Configurations;

internal sealed class AttachmentConfiguration : IEntityTypeConfiguration<Attachment>
{
    private const string AllowedContentTypesCheck =
        "content_type in (" +
        "'application/pdf'," +
        "'image/png'," +
        "'image/jpeg'," +
        "'application/vnd.openxmlformats-officedocument.wordprocessingml.document'," +
        "'application/vnd.openxmlformats-officedocument.presentationml.presentation'," +
        "'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet'" +
        ")";

    public void Configure(EntityTypeBuilder<Attachment> builder)
    {
        builder.ToTable("attachments", t =>
        {
            t.HasCheckConstraint("ck_attachment_size", "size_bytes > 0 and size_bytes <= 10485760");
            t.HasCheckConstraint("ck_attachment_content_type", AllowedContentTypesCheck);
        });

        builder.HasKey(a => a.Id);
        builder.Property(a => a.IdeaId).IsRequired();
        builder.Property(a => a.OriginalFileName).HasMaxLength(255).IsRequired();
        builder.Property(a => a.ContentType).HasMaxLength(100).IsRequired();
        builder.Property(a => a.SizeBytes).IsRequired();
        builder.Property(a => a.StorageKey).HasMaxLength(512).IsRequired();
        builder.Property(a => a.Sha256).HasColumnType("bytea").IsRequired();
        builder.Property(a => a.UploadedById).IsRequired();
        builder.Property(a => a.UploadedAt).IsRequired();

        // FR-010 — at most one attachment per idea (structurally enforced).
        builder.HasIndex(a => a.IdeaId).IsUnique().HasDatabaseName("ux_attachment_idea_id");
        builder.HasIndex(a => a.StorageKey).IsUnique().HasDatabaseName("ux_attachment_storage_key");

        builder.HasOne<Domain.Ideas.Idea>()
            .WithMany()
            .HasForeignKey(a => a.IdeaId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
