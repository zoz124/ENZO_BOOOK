using System.ComponentModel.DataAnnotations;

namespace BookNest.Core.Entities;

public class WishlistItem : BaseEntity
{
    [Required, MaxLength(450)]
    public string UserId { get; set; } = string.Empty;

    public int BookId { get; set; }
    public Book Book { get; set; } = null!;
}
