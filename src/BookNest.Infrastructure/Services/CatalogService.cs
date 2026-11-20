using System.Linq.Expressions;
using BookNest.Core.Constants;
using BookNest.Core.Entities;
using BookNest.Core.Interfaces.IServices;
using BookNest.Core.Models;
using BookNest.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace BookNest.Infrastructure.Services;

public class CatalogService : ICatalogService
{
    internal const string HomeCacheKey = "catalog:home";
    private static readonly TimeSpan HomeCacheDuration = TimeSpan.FromMinutes(10);

    internal static readonly Expression<Func<Book, BookCardDto>> CardProjection = b => new BookCardDto(
        b.Id,
        b.Title,
        b.Author.Name,
        b.Category.Name,
        b.Price,
        b.DiscountPrice,
        b.DiscountPrice ?? b.Price,
        b.Stock,
        b.CoverImageUrl,
        b.DiscountPrice != null,
        b.Reviews.Select(r => (double?)r.Rating).Average() ?? 0,
        b.Reviews.Count);

    private readonly ApplicationDbContext _context;
    private readonly IMemoryCache _cache;

    public CatalogService(ApplicationDbContext context, IMemoryCache cache)
    {
        _context = context;
        _cache = cache;
    }

    public async Task<PagedResult<BookCardDto>> GetBooksAsync(CatalogFilter filter, CancellationToken cancellationToken = default)
    {
        var query = BuildQuery(filter);

        var total = await query.CountAsync(cancellationToken);

        var pageSize = filter.PageSize is > 0 and <= 100 ? filter.PageSize : StoreSettings.CatalogPageSize;
        var page = Math.Max(1, filter.Page);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(CardProjection)
            .ToListAsync(cancellationToken);

        return new PagedResult<BookCardDto>(items, page, pageSize, total);
    }

    private IQueryable<Book> BuildQuery(CatalogFilter filter)
    {
        var query = _context.Books.AsNoTracking();

        if (filter.IncludeDeleted)
        {
            query = query.IgnoreQueryFilters();
        }

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            // T-67: compare normalized-to-normalized so "احمد" finds "أحمد" and
            // "مصطفي" finds "مصطفى". The column is maintained on save/seed.
            var term = Core.Utils.ArabicText.Normalize(filter.Search);

            query = query.Where(b =>
                EF.Functions.Like(b.SearchText, $"%{term}%") ||
                EF.Functions.Like(b.ISBN, $"%{filter.Search.Trim()}%"));
        }

        if (filter.CategoryId.HasValue)
        {
            query = query.Where(b => b.CategoryId == filter.CategoryId.Value);
        }

        if (filter.AuthorId.HasValue)
        {
            query = query.Where(b => b.AuthorId == filter.AuthorId.Value);
        }

        if (filter.MinPrice.HasValue)
        {
            query = query.Where(b => (b.DiscountPrice ?? b.Price) >= filter.MinPrice.Value);
        }

        if (filter.MaxPrice.HasValue)
        {
            query = query.Where(b => (b.DiscountPrice ?? b.Price) <= filter.MaxPrice.Value);
        }

        if (filter.InStockOnly)
        {
            query = query.Where(b => b.Stock > 0);
        }

        if (filter.LowStockOnly)
        {
            query = query.Where(b => b.Stock <= StoreSettings.LowStockThreshold);
        }

        return filter.Sort switch
        {
            CatalogSort.PriceAsc => query.OrderBy(b => b.DiscountPrice ?? b.Price).ThenBy(b => b.Id),
            CatalogSort.PriceDesc => query.OrderByDescending(b => b.DiscountPrice ?? b.Price).ThenBy(b => b.Id),
            CatalogSort.BestSelling => query
                .OrderByDescending(b => b.OrderItems.Sum(i => (int?)i.Quantity) ?? 0)
                .ThenBy(b => b.Id),
            _ => query.OrderByDescending(b => b.CreatedAt).ThenByDescending(b => b.Id)
        };
    }

    public async Task<BookDetailsDto?> GetDetailsAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Books
            .AsNoTracking()
            .Where(b => b.Id == id)
            .Select(b => new BookDetailsDto(
                b.Id,
                b.Title,
                b.ISBN,
                b.Description,
                b.Author.Name,
                b.AuthorId,
                b.Category.Name,
                b.CategoryId,
                b.Price,
                b.DiscountPrice,
                b.DiscountPrice ?? b.Price,
                b.Stock,
                b.Pages,
                b.Publisher,
                b.Language,
                b.PublishedDate,
                b.CoverImageUrl,
                b.Reviews.Select(r => (double?)r.Rating).Average() ?? 0,
                b.Reviews.Count))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<int?> GetStockAsync(int id, CancellationToken cancellationToken = default)
        => await _context.Books
            .AsNoTracking()
            .Where(b => b.Id == id)
            .Select(b => (int?)b.Stock)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<BookCardDto>> GetSimilarAsync(int bookId, int take = 4, CancellationToken cancellationToken = default)
    {
        var categoryId = await _context.Books
            .Where(b => b.Id == bookId)
            .Select(b => (int?)b.CategoryId)
            .FirstOrDefaultAsync(cancellationToken);

        if (categoryId is null)
        {
            return Array.Empty<BookCardDto>();
        }

        return await _context.Books
            .AsNoTracking()
            .Where(b => b.CategoryId == categoryId.Value && b.Id != bookId)
            .OrderByDescending(b => b.Stock > 0)
            .ThenByDescending(b => b.CreatedAt)
            .Take(take)
            .Select(CardProjection)
            .ToListAsync(cancellationToken);
    }

    public async Task<HomePageDto> GetHomePageAsync(CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue(HomeCacheKey, out HomePageDto? cached) && cached is not null)
        {
            return cached;
        }

        var newest = await _context.Books.AsNoTracking()
            .OrderByDescending(b => b.CreatedAt).ThenByDescending(b => b.Id)
            .Take(8).Select(CardProjection).ToListAsync(cancellationToken);

        var onSale = await _context.Books.AsNoTracking()
            .Where(b => b.DiscountPrice != null)
            .OrderByDescending(b => b.CreatedAt)
            .Take(8).Select(CardProjection).ToListAsync(cancellationToken);

        var bestSellerIds = await _context.OrderItems
            .GroupBy(i => i.BookId)
            .Select(g => new { BookId = g.Key, Units = g.Sum(i => i.Quantity) })
            .OrderByDescending(x => x.Units)
            .Take(8)
            .Select(x => x.BookId)
            .ToListAsync(cancellationToken);

        List<BookCardDto> bestSellers = new();
        if (bestSellerIds.Count > 0)
        {
            bestSellers = await _context.Books.AsNoTracking()
                .Where(b => bestSellerIds.Contains(b.Id))
                .Select(CardProjection).ToListAsync(cancellationToken);
        }

        var categories = await GetCategoriesAsync(cancellationToken);

        var dto = new HomePageDto(newest, bestSellers, onSale, categories);
        _cache.Set(HomeCacheKey, dto, HomeCacheDuration);

        return dto;
    }

    public async Task<IReadOnlyList<BookCardDto>> SuggestAsync(string term, int take = 8, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(term) || term.Trim().Length < 2)
        {
            return Array.Empty<BookCardDto>();
        }

        var value = Core.Utils.ArabicText.Normalize(term);

        return await _context.Books.AsNoTracking()
            .Where(b => EF.Functions.Like(b.SearchText, $"%{value}%"))
            .OrderBy(b => b.Title)
            .Take(take)
            .Select(CardProjection)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<LookupDto>> GetCategoriesAsync(CancellationToken cancellationToken = default)
        => await _context.Categories.AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new LookupDto(c.Id, c.Name, c.NameAr))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<LookupDto>> GetAuthorsAsync(CancellationToken cancellationToken = default)
        => await _context.Authors.AsNoTracking()
            .OrderBy(a => a.Name)
            .Select(a => new LookupDto(a.Id, a.Name, null))
            .ToListAsync(cancellationToken);

    public void InvalidateHomeCache() => _cache.Remove(HomeCacheKey);
}
