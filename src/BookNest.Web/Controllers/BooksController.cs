using System.Security.Claims;
using BookNest.Core.Interfaces.IServices;
using BookNest.Core.Models;
using BookNest.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookNest.Web.Controllers;

public class BooksController : Controller
{
    private readonly ICatalogService _catalog;
    private readonly IWishlistService _wishlist;
    private readonly IReviewService _reviews;

    public BooksController(ICatalogService catalog, IWishlistService wishlist, IReviewService reviews)
    {
        _catalog = catalog;
        _wishlist = wishlist;
        _reviews = reviews;
    }

    public async Task<IActionResult> Index(
        string? q,
        int? categoryId,
        int? authorId,
        decimal? minPrice,
        decimal? maxPrice,
        bool inStockOnly = false,
        CatalogSort sort = CatalogSort.Newest,
        int page = 1,
        CancellationToken cancellationToken = default)
    {
        var filter = new CatalogFilter
        {
            Search = q?.Trim(),
            CategoryId = categoryId,
            AuthorId = authorId,
            MinPrice = minPrice,
            MaxPrice = maxPrice,
            InStockOnly = inStockOnly,
            Sort = sort,
            Page = page,
            PageSize = 12
        };

        await LoadWishlistAsync(cancellationToken);

        var model = new CatalogViewModel
        {
            Filter = filter,
            Books = await _catalog.GetBooksAsync(filter, cancellationToken),
            Categories = await _catalog.GetCategoriesAsync(cancellationToken),
            Authors = await _catalog.GetAuthorsAsync(cancellationToken)
        };

        return View(model);
    }

    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var book = await _catalog.GetDetailsAsync(id, cancellationToken);
        if (book is null)
        {
            return NotFound();
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var model = new BookDetailsViewModel
        {
            Book = book,
            Similar = await _catalog.GetSimilarAsync(id, 4, cancellationToken),
            InWishlist = userId is not null && await _wishlist.ContainsAsync(userId, id, cancellationToken),
            Reviews = await _reviews.GetForBookAsync(id, userId, cancellationToken),
            IsSignedIn = userId is not null
        };

        await LoadWishlistAsync(cancellationToken);

        return View(model);
    }

    /// <summary>
    /// Live stock for the details page. Polled by the browser so two shoppers never
    /// both believe the last copy is theirs; the real guard is the conditional
    /// decrement inside the order transaction.
    /// </summary>
    [HttpGet]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public async Task<IActionResult> Stock(int id, CancellationToken cancellationToken)
    {
        var stock = await _catalog.GetStockAsync(id, cancellationToken);

        if (stock is null)
        {
            return NotFound();
        }

        return Json(new { id, stock, inStock = stock > 0 });
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddReview(int id, int rating, string? comment, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await _reviews.AddAsync(id, userId, rating, comment, cancellationToken);

        TempData[result.Succeeded ? "Success" : "Error"] = result.Message;

        return Redirect(Url.Action(nameof(Details), new { id }) + "#reviews");
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteReview(int id, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await _reviews.DeleteAsync(id, userId, cancellationToken);

        TempData[result.Succeeded ? "Success" : "Error"] = result.Message;

        return Redirect(Url.Action(nameof(Details), new { id }) + "#reviews");
    }

    [HttpGet]
    public async Task<IActionResult> Suggest(string term, CancellationToken cancellationToken)
        => Json(await _catalog.SuggestAsync(term, 8, cancellationToken));

    private async Task LoadWishlistAsync(CancellationToken cancellationToken)
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return;
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is not null)
        {
            ViewData["WishlistIds"] = await _wishlist.GetIdsAsync(userId, cancellationToken);
        }
    }
}
