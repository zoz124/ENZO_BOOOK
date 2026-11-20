using System.ComponentModel.DataAnnotations;

namespace BookNest.Core.Entities;

public class CartItem : BaseEntity
{
    [MaxLength(450)]
    public string? UserId { get; set; } // null for guest

    public int BookId { get; set; }
    public Book Book { get; set; } = null!;

    public int Quantity { get; set; }

    [MaxLength(100)]
    public string? SessionId { get; set; } // للزوار
}
