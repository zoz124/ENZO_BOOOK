using System.ComponentModel.DataAnnotations;

namespace BookNest.Core.Entities;

public class Author : BaseEntity
{
    [Required, MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Biography { get; set; }

    [MaxLength(500)]
    public string? PhotoUrl { get; set; }

    // Navigation
    public ICollection<Book> Books { get; set; } = new List<Book>();
}
