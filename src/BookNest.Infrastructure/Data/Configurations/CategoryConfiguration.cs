using BookNest.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookNest.Infrastructure.Data.Configurations;

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.HasKey(c => c.Id);

        builder.HasIndex(c => c.Name)
            .IsUnique()
            .HasDatabaseName("IX_Categories_Name_Unique");

        builder.HasIndex(c => c.Slug)
            .IsUnique()
            .HasFilter("[Slug] IS NOT NULL")
            .HasDatabaseName("IX_Categories_Slug_Unique");
    }
}
