using BookNest.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookNest.Infrastructure.Data.Configurations;

public class BookConfiguration : IEntityTypeConfiguration<Book>
{
    public void Configure(EntityTypeBuilder<Book> builder)
    {
        builder.HasKey(b => b.Id);

        // Unique constraint on ISBN (BR-05)
        builder.HasIndex(b => b.ISBN)
            .IsUnique()
            .HasDatabaseName("IX_Books_ISBN_Unique");

        // Data integrity: money and stock can never go negative, whatever path wrote them.
        builder.ToTable(t => t.HasCheckConstraint(
            "CK_Books_Price_NonNegative",
            "[Price] >= 0 AND ([DiscountPrice] IS NULL OR [DiscountPrice] >= 0)"));

        builder.ToTable(t => t.HasCheckConstraint(
            "CK_Books_Stock_NonNegative",
            "[Stock] >= 0 AND [Reserved] >= 0"));

        // Concurrency token
        builder.Property(b => b.RowVersion)
            .IsRowVersion();

        // Decimal precision
        builder.Property(b => b.Price)
            .HasPrecision(18, 2);

        builder.Property(b => b.DiscountPrice)
            .HasPrecision(18, 2);

        // Relationships
        builder.HasOne(b => b.Author)
            .WithMany(a => a.Books)
            .HasForeignKey(b => b.AuthorId)
            .OnDelete(DeleteBehavior.Restrict); // منع حذف مؤلف له كتب (BR-03)

        builder.HasOne(b => b.Category)
            .WithMany(c => c.Books)
            .HasForeignKey(b => b.CategoryId)
            .OnDelete(DeleteBehavior.Restrict); // منع حذف تصنيف له كتب
    }
}
