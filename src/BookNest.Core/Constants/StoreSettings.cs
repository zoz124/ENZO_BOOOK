namespace BookNest.Core.Constants;

/// <summary>Store-wide business settings (BR-06).</summary>
public static class StoreSettings
{
    public const decimal ShippingCost = 50m;
    public const decimal FreeShippingThreshold = 500m;
    public const int CatalogPageSize = 12;
    public const int AdminPageSize = 25;
    public const int LowStockThreshold = 10;
    public const int MaxUploadBytes = 2 * 1024 * 1024;
    public const string CurrencySymbol = "EGP";

    public static decimal ShippingFor(decimal subtotal) =>
        subtotal >= FreeShippingThreshold ? 0m : ShippingCost;
}
