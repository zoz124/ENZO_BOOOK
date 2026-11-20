using BookNest.Core.Entities;
using BookNest.Core.Interfaces.IServices;
using BookNest.Infrastructure.Data;
using BookNest.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BookNest.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class CategoriesController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ICatalogService _catalog;

    public CategoriesController(ApplicationDbContext context, ICatalogService catalog)
    {
        _context = context;
        _catalog = catalog;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
        => View(await _context.Categories.AsNoTracking().OrderBy(c => c.Name).ToListAsync(cancellationToken));

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(SimpleNameFormViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid || string.IsNullOrWhiteSpace(model.Name))
        {
            TempData["Error"] = "A category name is required.";
            return RedirectToAction(nameof(Index));
        }

        var nameTaken = await _context.Categories
            .AnyAsync(c => c.Name == model.Name && c.Id != (model.Id ?? 0), cancellationToken);

        if (nameTaken)
        {
            TempData["Error"] = "A category with that name already exists.";
            return RedirectToAction(nameof(Index));
        }

        Category category;

        if (model.Id is > 0)
        {
            category = await _context.Categories.FirstOrDefaultAsync(c => c.Id == model.Id, cancellationToken)
                       ?? new Category();
            if (category.Id == 0)
            {
                _context.Categories.Add(category);
            }
        }
        else
        {
            category = new Category();
            _context.Categories.Add(category);
        }

        category.Name = model.Name.Trim();
        category.NameAr = string.IsNullOrWhiteSpace(model.NameAr) ? model.Name.Trim() : model.NameAr.Trim();
        category.Description = model.Description;
        category.Slug = Slugify(model.Name);
        category.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        _catalog.InvalidateHomeCache();
        TempData["Success"] = "Category saved.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var hasBooks = await _context.Books.IgnoreQueryFilters().AnyAsync(b => b.CategoryId == id, cancellationToken);
        if (hasBooks)
        {
            TempData["Error"] = "You cannot delete a category that still has books (BR-03).";
            return RedirectToAction(nameof(Index));
        }

        var category = await _context.Categories.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (category is not null)
        {
            _context.Categories.Remove(category);
            await _context.SaveChangesAsync(cancellationToken);
            _catalog.InvalidateHomeCache();
            TempData["Success"] = "Category deleted.";
        }

        return RedirectToAction(nameof(Index));
    }

    private static string Slugify(string value)
        => new string(value.Trim().ToLowerInvariant().Select(c => char.IsLetterOrDigit(c) ? c : '-').ToArray());
}

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class AuthorsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ICatalogService _catalog;

    public AuthorsController(ApplicationDbContext context, ICatalogService catalog)
    {
        _context = context;
        _catalog = catalog;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
        => View(await _context.Authors.AsNoTracking().OrderBy(a => a.Name).ToListAsync(cancellationToken));

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(SimpleNameFormViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid || string.IsNullOrWhiteSpace(model.Name))
        {
            TempData["Error"] = "An author name is required.";
            return RedirectToAction(nameof(Index));
        }

        Author author;

        if (model.Id is > 0)
        {
            author = await _context.Authors.FirstOrDefaultAsync(a => a.Id == model.Id, cancellationToken)
                     ?? new Author();
            if (author.Id == 0)
            {
                _context.Authors.Add(author);
            }
        }
        else
        {
            author = new Author();
            _context.Authors.Add(author);
        }

        author.Name = model.Name.Trim();
        author.Biography = model.Biography;
        author.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        // Renaming an author invalidates the denormalized search text of their books (T-67).
        var authorId = author.Id;
        foreach (var book in await _context.Books.Where(b => b.AuthorId == authorId).ToListAsync(cancellationToken))
        {
            book.SearchText = BookNest.Core.Utils.ArabicText.BuildSearchText(book.Title, author.Name, book.ISBN);
        }

        await _context.SaveChangesAsync(cancellationToken);
        _catalog.InvalidateHomeCache();
        TempData["Success"] = "Category saved.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var hasBooks = await _context.Books.IgnoreQueryFilters().AnyAsync(b => b.AuthorId == id, cancellationToken);
        if (hasBooks)
        {
            TempData["Error"] = "You cannot delete an author who still has books (BR-03).";
            return RedirectToAction(nameof(Index));
        }

        var author = await _context.Authors.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
        if (author is not null)
        {
            _context.Authors.Remove(author);
            await _context.SaveChangesAsync(cancellationToken);
            _catalog.InvalidateHomeCache();
            TempData["Success"] = "Author deleted.";
        }

        return RedirectToAction(nameof(Index));
    }
}
