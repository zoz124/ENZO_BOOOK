using System.ComponentModel.DataAnnotations;
using BookNest.Core.Entities;
using Microsoft.AspNetCore.Identity;

namespace BookNest.Infrastructure.Identity;

public class ApplicationUser : IdentityUser
{
    [MaxLength(100)]
    public string? FirstName { get; set; }

    [MaxLength(100)]
    public string? LastName { get; set; }

    public DateTime? DateOfBirth { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation
    public ICollection<Order> Orders { get; set; } = new List<Order>();
    public ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
    public ICollection<WishlistItem> WishlistItems { get; set; } = new List<WishlistItem>();
    public ICollection<Address> Addresses { get; set; } = new List<Address>();
    public ICollection<StockReservation> Reservations { get; set; } = new List<StockReservation>();
}
