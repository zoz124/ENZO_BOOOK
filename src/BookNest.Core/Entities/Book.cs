using System.ComponentModel.DataAnnotations;

namespace BookNest.Core.Entities;

public class Book : BaseEntity
{
    [Required, MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required, MaxLength(13)]
    public string ISBN { get; set; } = string.Empty; // Unique index (BR-05)

    /// <summary>Normalized title + author + ISBN, maintained for Arabic-aware search (T-67).</summary>
    [MaxLength(600)]
    public string SearchText { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Description { get; set; }

    [Required]
    public decimal Price { get; set; }

    public decimal? DiscountPrice { get; set; }

    [Required]
    public int Stock { get; set; }

    public int Reserved { get; set; } = 0; // للحجز المؤقت (Wow Layer)

    [MaxLength(500)]
    public string? CoverImageUrl { get; set; }

    public int? Pages { get; set; }

    [MaxLength(100)]
    public string? Publisher { get; set; }

    [MaxLength(10)]
    public string? Language { get; set; }

    public DateTime? PublishedDate { get; set; }

    public bool IsDeleted { get; set; } = false; // Soft delete (BR-07)

    // Relationships
    public int AuthorId { get; set; }
    public Author Author { get; set; } = null!;

    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;

    // Concurrency token
    [Timestamp]
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    // Collections
    public ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    public ICollection<WishlistItem> WishlistItems { get; set; } = new List<WishlistItem>();
    public ICollection<StockReservation> Reservations { get; set; } = new List<StockReservation>();
    public ICollection<BookReview> Reviews { get; set; } = new List<BookReview>();
}
