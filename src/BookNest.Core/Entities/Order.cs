using System.ComponentModel.DataAnnotations;
using BookNest.Core.Enums;

namespace BookNest.Core.Entities;

public class Order : BaseEntity
{
    [Required, MaxLength(50)]
    public string OrderNumber { get; set; } = string.Empty; // unique

    /// <summary>Client-supplied key that makes order submission idempotent (T-41).</summary>
    [MaxLength(64)]
    public string? IdempotencyKey { get; set; }

    [Required, MaxLength(450)]
    public string UserId { get; set; } = string.Empty;

    public OrderStatus Status { get; set; } = OrderStatus.Pending;

    [Required]
    public decimal Subtotal { get; set; }

    [Required]
    public decimal ShippingCost { get; set; }

    [Required]
    public decimal Total { get; set; }

    // Shipping Address (snapshot)
    [Required, MaxLength(200)]
    public string ShippingName { get; set; } = string.Empty;

    [Required, MaxLength(15)]
    public string ShippingPhone { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string ShippingCity { get; set; } = string.Empty;

    [Required, MaxLength(500)]
    public string ShippingAddress { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? ShippingPostalCode { get; set; }

    [MaxLength(50)]
    public string PaymentMethod { get; set; } = "Cash"; // Cash, Card

    [MaxLength(100)]
    public string? PaymentTransactionId { get; set; }

    public DateTime? PaidAt { get; set; }
    public DateTime? ShippedAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public DateTime? CancelledAt { get; set; }

    [MaxLength(500)]
    public string? CancellationReason { get; set; }

    // Concurrency
    [Timestamp]
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    // Navigation
    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
    public ICollection<OrderStatusHistory> StatusHistory { get; set; } = new List<OrderStatusHistory>();
}
