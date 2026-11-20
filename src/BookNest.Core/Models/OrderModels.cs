using BookNest.Core.Enums;

namespace BookNest.Core.Models;

public sealed class CheckoutRequest
{
    public int? AddressId { get; set; }
    public string? FullName { get; set; }
    public string? Phone { get; set; }
    public string? City { get; set; }
    public string? AddressLine { get; set; }
    public string? PostalCode { get; set; }
    public bool SaveAddress { get; set; }
    public string PaymentMethod { get; set; } = "Cash";
    public string? IdempotencyKey { get; set; }
}

public sealed record OrderResult(bool Succeeded, int? OrderId = null, string? Error = null)
{
    public static OrderResult Success(int orderId) => new(true, orderId);
    public static OrderResult Failure(string error) => new(false, null, error);
}

public sealed record OrderSummaryDto(
    int Id,
    string OrderNumber,
    DateTime CreatedAt,
    OrderStatus Status,
    decimal Total,
    int ItemCount,
    string CustomerName);

public sealed record OrderLineDto(
    string BookTitle,
    string BookIsbn,
    decimal UnitPrice,
    int Quantity,
    decimal Subtotal);

public sealed record OrderDetailsDto(
    int Id,
    string OrderNumber,
    DateTime CreatedAt,
    OrderStatus Status,
    decimal Subtotal,
    decimal ShippingCost,
    decimal Total,
    string ShippingName,
    string ShippingPhone,
    string ShippingCity,
    string ShippingAddress,
    string? ShippingPostalCode,
    string PaymentMethod,
    string CustomerName,
    string CustomerEmail,
    IReadOnlyList<OrderLineDto> Lines,
    IReadOnlyList<OrderStatusChangeDto> History)
{
    public bool CanCancel => Status == OrderStatus.Pending;

    public string PaymentLabel => PaymentMethod == "Visa" ? "Visa / Card" : "Cash on delivery";
}

public sealed record OrderStatusChangeDto(OrderStatus From, OrderStatus To, DateTime ChangedAt, string? ChangedBy, string? Notes);

public sealed record AddressDto(int Id, string FullName, string Phone, string City, string AddressLine, string? PostalCode, bool IsDefault);

public sealed record DashboardStatsDto(
    decimal TotalSales,
    int OrdersCount,
    int NewOrders,
    IReadOnlyList<BookCardDto> LowStockBooks,
    IReadOnlyList<DailySalesDto> SalesLast30Days,
    IReadOnlyList<TopBookDto> TopBooks,
    IReadOnlyList<CategorySalesDto> CategorySales,
    int OutOfStockCount,
    int ReviewsCount,
    double AverageRating,
    IReadOnlyList<AdminReviewDto> RecentReviews);

public sealed record CategorySalesDto(string CategoryName, int UnitsSold, decimal Revenue);

public sealed record DailySalesDto(DateTime Day, decimal Total);

public sealed record TopBookDto(string Title, int UnitsSold, decimal Revenue);

public sealed record AdminUserDto(string Id, string Email, string FullName, bool IsActive, IReadOnlyList<string> Roles, DateTime CreatedAt);
