using BookNest.Core.Interfaces.IServices;
using BookNest.Core.Models;

namespace BookNest.IntegrationTests;

/// <summary>
/// Smoke tests only prove the app boots and the shell renders, so the DB-backed
/// services are replaced with empty stubs. Database behaviour is covered by the
/// Testcontainers tests.
/// </summary>
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

internal sealed class StubCartService : ICartService
{
    private static readonly CartSummary Empty = new(Array.Empty<CartLine>(), 0m, 0m, 0);

    public Task<CartSummary> GetAsync(CancellationToken cancellationToken = default) => Task.FromResult(Empty);

    public Task<CartOperationResult> AddAsync(int bookId, int quantity, CancellationToken cancellationToken = default)
        => Task.FromResult(CartOperationResult.Ok(0));

    public Task<CartOperationResult> UpdateAsync(int bookId, int quantity, CancellationToken cancellationToken = default)
        => Task.FromResult(CartOperationResult.Ok(0));

    public Task<CartOperationResult> RemoveAsync(int bookId, CancellationToken cancellationToken = default)
        => Task.FromResult(CartOperationResult.Ok(0));

    public Task MergeGuestCartAsync(string userId, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task ClearAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}
