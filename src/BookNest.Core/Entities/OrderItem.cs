using System.ComponentModel.DataAnnotations;

namespace BookNest.Core.Entities;

public class OrderItem : BaseEntity
{
    public int OrderId { get; set; }
    public Order Order { get; set; } = null!;

    public int BookId { get; set; }
    public Book Book { get; set; } = null!;

    // Snapshot at order time (BR-04)
    [Required, MaxLength(200)]
    public string BookTitle { get; set; } = string.Empty;

    [Required, MaxLength(13)]
    public string BookISBN { get; set; } = string.Empty;

    [Required]
    public decimal UnitPrice { get; set; } // السعر وقت الطلب

    [Required]
    public int Quantity { get; set; }

    [Required]
    public decimal Subtotal { get; set; } // UnitPrice * Quantity
}
