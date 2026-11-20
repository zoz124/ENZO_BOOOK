using System.ComponentModel.DataAnnotations;

namespace BookNest.Core.Entities;

public class Address : BaseEntity
{
    [Required, MaxLength(450)]
    public string UserId { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required, MaxLength(15)]
    public string Phone { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string City { get; set; } = string.Empty;

    [Required, MaxLength(500)]
    public string AddressLine { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? PostalCode { get; set; }

    public bool IsDefault { get; set; } = false;
}
