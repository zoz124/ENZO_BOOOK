namespace BookNest.Core.Models;

public sealed record CartLine(
    int BookId,
    string Title,
    string AuthorName,
    string? CoverImageUrl,
    decimal UnitPrice,
    int Quantity,
    int Stock,
    bool IsAvailable)
{
    public decimal LineTotal => UnitPrice * Quantity;
}

public sealed record CartSummary(
    IReadOnlyList<CartLine> Lines,
    decimal Subtotal,
    decimal Shipping,
    int TotalItems)
{
    public decimal Total => Subtotal + Shipping;
    public bool IsEmpty => Lines.Count == 0;
}

public sealed record CartOperationResult(bool Succeeded, string? Message = null, int TotalItems = 0)
{
    public static CartOperationResult Ok(int totalItems, string? message = null) => new(true, message, totalItems);
    public static CartOperationResult Fail(string message, int totalItems = 0) => new(false, message, totalItems);
}

public sealed record WishlistItemDto(int BookId, string Title, string AuthorName, decimal Price, decimal EffectivePrice, int Stock, string? CoverImageUrl);
