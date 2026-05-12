using InnovatEpam.Portal.Domain.Categories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InnovatEpam.Portal.Infrastructure.Persistence.Configurations;

internal sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("categories");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Code).HasMaxLength(32).IsRequired();
        builder.Property(c => c.Name).HasMaxLength(64).IsRequired();
        builder.Property(c => c.IsActive).HasDefaultValue(true);
        builder.Property(c => c.SortOrder).HasDefaultValue(0);

        builder.HasIndex(c => c.Code).IsUnique().HasDatabaseName("ux_category_code");
        builder.HasIndex(c => c.Name).IsUnique().HasDatabaseName("ux_category_name");
    }
}
