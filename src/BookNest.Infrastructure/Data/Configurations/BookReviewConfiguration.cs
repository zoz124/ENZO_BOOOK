using BookNest.Core.Entities;
using BookNest.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookNest.Infrastructure.Data.Configurations;

public class BookReviewConfiguration : IEntityTypeConfiguration<BookReview>
{
    public void Configure(EntityTypeBuilder<BookReview> builder)
    {
        builder.HasKey(r => r.Id);

        // Ratings are one to five stars, enforced in the database as well as the app.
        builder.ToTable(t => t.HasCheckConstraint(
            "CK_BookReviews_Rating_Range",
            "[Rating] BETWEEN 1 AND 5"));

        // One rating per customer per book (FR-REV-01).
        builder.HasIndex(r => new { r.UserId, r.BookId })
            .IsUnique()
            .HasDatabaseName("IX_BookReviews_UserId_BookId_Unique");

        builder.HasIndex(r => r.BookId)
            .HasDatabaseName("IX_BookReviews_BookId");

        builder.HasOne(r => r.Book)
            .WithMany(b => b.Reviews)
            .HasForeignKey(r => r.BookId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
