using BookNest.Core.Entities;
using BookNest.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookNest.Infrastructure.Data.Configurations;

public class CartItemConfiguration : IEntityTypeConfiguration<CartItem>
{
    public void Configure(EntityTypeBuilder<CartItem> builder)
    {
        builder.HasKey(c => c.Id);

        // A cart line with zero or negative copies is always a bug, never a state.
        builder.ToTable(t => t.HasCheckConstraint(
            "CK_CartItems_Quantity_Positive",
            "[Quantity] > 0"));

        // Unique constraint: one book per user (no duplicates)
        builder.HasIndex(c => new { c.UserId, c.BookId })
            .IsUnique()
            .HasFilter("[UserId] IS NOT NULL")
            .HasDatabaseName("IX_CartItems_UserId_BookId_Unique");

        // Unique constraint: one book per session
        builder.HasIndex(c => new { c.SessionId, c.BookId })
            .IsUnique()
            .HasFilter("[SessionId] IS NOT NULL")
            .HasDatabaseName("IX_CartItems_SessionId_BookId_Unique");

        // Relationships
        builder.HasOne(c => c.Book)
            .WithMany(b => b.CartItems)
            .HasForeignKey(c => c.BookId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<ApplicationUser>()
            .WithMany(u => u.CartItems)
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
