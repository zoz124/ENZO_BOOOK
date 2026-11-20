using BookNest.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookNest.Infrastructure.Data.Configurations;

public class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.HasKey(i => i.Id);

        // Decimal precision
        builder.Property(i => i.UnitPrice).HasPrecision(18, 2);
        builder.Property(i => i.Subtotal).HasPrecision(18, 2);

        // Relationships
        builder.HasOne(i => i.Book)
            .WithMany(b => b.OrderItems)
            .HasForeignKey(i => i.BookId)
            .OnDelete(DeleteBehavior.Restrict); // Keep order history even if book deleted
    }
}
