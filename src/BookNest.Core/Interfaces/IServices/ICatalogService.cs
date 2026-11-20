using BookNest.Core.Models;

namespace BookNest.Core.Interfaces.IServices;

public interface ICatalogService
{
    Task<PagedResult<BookCardDto>> GetBooksAsync(CatalogFilter filter, CancellationToken cancellationToken = default);
    Task<BookDetailsDto?> GetDetailsAsync(int id, CancellationToken cancellationToken = default);
    Task<int?> GetStockAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BookCardDto>> GetSimilarAsync(int bookId, int take = 4, CancellationToken cancellationToken = default);
    Task<HomePageDto> GetHomePageAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BookCardDto>> SuggestAsync(string term, int take = 8, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LookupDto>> GetCategoriesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LookupDto>> GetAuthorsAsync(CancellationToken cancellationToken = default);
    void InvalidateHomeCache();
}
