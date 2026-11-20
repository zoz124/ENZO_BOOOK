using System.ComponentModel.DataAnnotations;

namespace BookNest.Core.Entities;

public class StockReservation : BaseEntity
{
    [Required, MaxLength(450)]
    public string UserId { get; set; } = string.Empty;

    public int BookId { get; set; }
    public Book Book { get; set; } = null!;

    public int Quantity { get; set; }

    public DateTime ExpiresAt { get; set; }

    public bool IsActive { get; set; } = true;

    [MaxLength(100)]
    public string? SessionId { get; set; }
}
