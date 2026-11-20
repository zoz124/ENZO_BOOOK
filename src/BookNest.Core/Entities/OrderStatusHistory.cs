using System.ComponentModel.DataAnnotations;
using BookNest.Core.Enums;

namespace BookNest.Core.Entities;

public class OrderStatusHistory : BaseEntity
{
    public int OrderId { get; set; }
    public Order Order { get; set; } = null!;

    public OrderStatus FromStatus { get; set; }
    public OrderStatus ToStatus { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    [MaxLength(450)]
    public string? ChangedByUserId { get; set; }

    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
}
