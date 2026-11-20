using System.ComponentModel.DataAnnotations;

namespace BookNest.Core.Entities;

public class Category : BaseEntity
{
    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string NameAr { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    [MaxLength(50)]
    public string? Slug { get; set; }

    // Navigation
    public ICollection<Book> Books { get; set; } = new List<Book>();
}
