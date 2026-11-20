using BookNest.Core.Interfaces.IServices;
using BookNest.Core.Models;

namespace BookStore.Tests;

internal sealed class StubCatalogService : ICatalogService
{
    public Task<PagedResult<BookCardDto>> GetBooksAsync(CatalogFilter filter, CancellationToken cancellationToken = default)
        => Task.FromResult(PagedResult<BookCardDto>.Empty(filter.PageSize));

    public Task<BookDetailsDto?> GetDetailsAsync(int id, CancellationToken cancellationToken = default)
        => Task.FromResult<BookDetailsDto?>(null);

    public Task<int?> GetStockAsync(int id, CancellationToken cancellationToken = default)
        => Task.FromResult<int?>(0);

    public Task<IReadOnlyList<BookCardDto>> GetSimilarAsync(int bookId, int take = 4, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<BookCardDto>>(Array.Empty<BookCardDto>());

    public Task<HomePageDto> GetHomePageAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(new HomePageDto(
            Array.Empty<BookCardDto>(),
            Array.Empty<BookCardDto>(),
            Array.Empty<BookCardDto>(),
            Array.Empty<LookupDto>()));

    public Task<IReadOnlyList<BookCardDto>> SuggestAsync(string term, int take = 8, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<BookCardDto>>(Array.Empty<BookCardDto>());

    public Task<IReadOnlyList<LookupDto>> GetCategoriesAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<LookupDto>>(Array.Empty<LookupDto>());

    public Task<IReadOnlyList<LookupDto>> GetAuthorsAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<LookupDto>>(Array.Empty<LookupDto>());

    public void InvalidateHomeCache()
    {
    }
}
