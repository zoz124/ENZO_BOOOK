using BookNest.Core.Entities;
using BookNest.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookNest.Infrastructure.Data.Configurations;

public class WishlistItemConfiguration : IEntityTypeConfiguration<WishlistItem>
{
    public void Configure(EntityTypeBuilder<WishlistItem> builder)
    {
        builder.HasKey(w => w.Id);

        // Unique: one book per user wishlist
        builder.HasIndex(w => new { w.UserId, w.BookId })
            .IsUnique()
            .HasDatabaseName("IX_WishlistItems_UserId_BookId_Unique");

        builder.HasOne(w => w.Book)
            .WithMany(b => b.WishlistItems)
            .HasForeignKey(w => w.BookId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<ApplicationUser>()
            .WithMany(u => u.WishlistItems)
            .HasForeignKey(w => w.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
