using BookNest.Core.Entities;
using BookNest.Core.Interfaces.IServices;
using BookNest.Core.Models;
using BookNest.Infrastructure.Data;
using BookNest.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BookNest.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class BooksController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ICatalogService _catalog;
    private readonly IImageStorage _images;

    public BooksController(ApplicationDbContext context, ICatalogService catalog, IImageStorage images)
    {
        _context = context;
        _catalog = catalog;
        _images = images;
    }

    public async Task<IActionResult> Index(string? q, bool lowStock = false, int page = 1, CancellationToken cancellationToken = default)
    {
        var filter = new CatalogFilter
        {
            Search = q,
            IncludeDeleted = true,
            LowStockOnly = lowStock,
            Page = page,
            PageSize = 25,
            Sort = CatalogSort.Newest
        };

        ViewData["Search"] = q;
        ViewData["LowStock"] = lowStock;

        return View(await _catalog.GetBooksAsync(filter, cancellationToken));
    }

    [HttpGet]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
        => View("Form", await BuildFormAsync(new AdminBookFormViewModel(), cancellationToken));

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var book = await _context.Books.IgnoreQueryFilters().FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
        if (book is null)
        {
            return NotFound();
        }

        var model = new AdminBookFormViewModel
        {
            Id = book.Id,
            Title = book.Title,
            Isbn = book.ISBN,
            Description = book.Description,
            Price = book.Price,
            DiscountPrice = book.DiscountPrice,
            Stock = book.Stock,
            Pages = book.Pages,
            Publisher = book.Publisher,
            Language = book.Language,
            PublishedDate = book.PublishedDate,
            AuthorId = book.AuthorId,
            CategoryId = book.CategoryId,
            CoverImageUrl = book.CoverImageUrl
        };

        return View("Form", await BuildFormAsync(model, cancellationToken));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(AdminBookFormViewModel model, CancellationToken cancellationToken)
    {
        if (model.DiscountPrice.HasValue && model.DiscountPrice.Value >= model.Price)
        {
            ModelState.AddModelError(nameof(model.DiscountPrice), "The discount price must be lower than the list price (BR-03).");
        }

        var duplicateIsbn = await _context.Books.IgnoreQueryFilters()
            .AnyAsync(b => b.ISBN == model.Isbn && b.Id != (model.Id ?? 0), cancellationToken);

        if (duplicateIsbn)
        {
            ModelState.AddModelError(nameof(model.Isbn), "That ISBN already belongs to another book (BR-05).");
        }

        if (!ModelState.IsValid)
        {
            return View("Form", await BuildFormAsync(model, cancellationToken));
        }

        string? coverUrl = model.CoverImageUrl;

        if (model.Cover is not null && model.Cover.Length > 0)
        {
            await using var stream = model.Cover.OpenReadStream();
            var saved = await _images.SaveCoverAsync(stream, model.Cover.FileName, cancellationToken);

            if (saved is null)
            {
                ModelState.AddModelError(nameof(model.Cover), "The cover must be a JPG, PNG or WebP file under 2 MB.");
                return View("Form", await BuildFormAsync(model, cancellationToken));
            }

            coverUrl = saved;
        }

        Book book;

        if (model.Id is > 0)
        {
            book = await _context.Books.IgnoreQueryFilters().FirstAsync(b => b.Id == model.Id, cancellationToken);
        }
        else
        {
            book = new Book();
            _context.Books.Add(book);
        }

        book.Title = model.Title.Trim();
        book.ISBN = model.Isbn.Trim();
        book.Description = model.Description;
        book.Price = model.Price;
        book.DiscountPrice = model.DiscountPrice;
        book.Stock = model.Stock;
        book.Pages = model.Pages;
        book.Publisher = model.Publisher;
        book.Language = model.Language;
        book.PublishedDate = model.PublishedDate;
        book.AuthorId = model.AuthorId;
        book.CategoryId = model.CategoryId;
        book.CoverImageUrl = coverUrl;
        book.SearchText = BookNest.Core.Utils.ArabicText.BuildSearchText(
            model.Title, (await _context.Authors.Where(a => a.Id == model.AuthorId).Select(a => a.Name).FirstOrDefaultAsync(cancellationToken)), model.Isbn);
        book.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        _catalog.InvalidateHomeCache();

        TempData["Success"] = model.Id is > 0 ? "Book updated." : "Book added.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var book = await _context.Books.IgnoreQueryFilters().FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
        if (book is null)
        {
            return NotFound();
        }

        // BR-07: soft delete keeps order history intact.
        book.IsDeleted = true;
        book.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
        _catalog.InvalidateHomeCache();

        TempData["Success"] = "Book archived (soft delete) — past orders keep their history.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Restore(int id, CancellationToken cancellationToken)
    {
        var book = await _context.Books.IgnoreQueryFilters().FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
        if (book is null)
        {
            return NotFound();
        }

        book.IsDeleted = false;
        book.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
        _catalog.InvalidateHomeCache();

        TempData["Success"] = "Book restored.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<AdminBookFormViewModel> BuildFormAsync(AdminBookFormViewModel model, CancellationToken cancellationToken)
    {
        model.Categories = await _catalog.GetCategoriesAsync(cancellationToken);
        model.Authors = await _catalog.GetAuthorsAsync(cancellationToken);
        return model;
    }
}
