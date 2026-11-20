namespace BookNest.Core.Models;

public enum CatalogSort
{
    Newest = 0,
    PriceAsc = 1,
    PriceDesc = 2,
    BestSelling = 3
}

public sealed class CatalogFilter
{
    public string? Search { get; set; }
    public int? CategoryId { get; set; }
    public int? AuthorId { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public bool InStockOnly { get; set; }
    public CatalogSort Sort { get; set; } = CatalogSort.Newest;
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 12;
    public bool IncludeDeleted { get; set; }
    public bool LowStockOnly { get; set; }
}

public sealed record BookCardDto(
    int Id,
    string Title,
    string AuthorName,
    string CategoryName,
    decimal Price,
    decimal? DiscountPrice,
    decimal EffectivePrice,
    int Stock,
    string? CoverImageUrl,
    bool IsDiscounted,
    double AverageRating,
    int ReviewCount)
{
    public bool HasReviews => ReviewCount > 0;
}

public sealed record BookDetailsDto(
    int Id,
    string Title,
    string Isbn,
    string? Description,
    string AuthorName,
    int AuthorId,
    string CategoryName,
    int CategoryId,
    decimal Price,
    decimal? DiscountPrice,
    decimal EffectivePrice,
    int Stock,
    int? Pages,
    string? Publisher,
    string? Language,
    DateTime? PublishedDate,
    string? CoverImageUrl,
    double AverageRating,
    int ReviewCount);

public sealed record LookupDto(int Id, string Name, string? NameAr = null)
{
    /// <summary>Arabic label when the UI is Arabic and a translation exists.</summary>
    public string DisplayName(bool arabic) => arabic && !string.IsNullOrWhiteSpace(NameAr) ? NameAr! : Name;
}

public sealed record HomePageDto(
    IReadOnlyList<BookCardDto> Newest,
    IReadOnlyList<BookCardDto> BestSellers,
    IReadOnlyList<BookCardDto> OnSale,
    IReadOnlyList<LookupDto> Categories);
