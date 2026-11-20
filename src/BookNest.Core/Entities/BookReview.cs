using System.ComponentModel.DataAnnotations;

namespace BookNest.Core.Entities;

/// <summary>
/// One review per customer per book (unique index on UserId + BookId).
/// The book rating shown in the store is the average of these rows.
/// </summary>
public class BookReview : BaseEntity
{
    [Required, MaxLength(450)]
    public string UserId { get; set; } = string.Empty;

    public int BookId { get; set; }
    public Book Book { get; set; } = null!;

    [Range(1, 5)]
    public int Rating { get; set; }

    [MaxLength(1000)]
    public string? Comment { get; set; }

    [Required, MaxLength(100)]
    public string ReviewerName { get; set; } = string.Empty;
}
