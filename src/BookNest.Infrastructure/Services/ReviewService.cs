using BookNest.Core.Entities;
using BookNest.Core.Interfaces.IServices;
using BookNest.Core.Models;
using BookNest.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BookNest.Infrastructure.Services;

public class ReviewService : IReviewService
{
    private readonly ApplicationDbContext _context;
    private readonly ICatalogService _catalog;
    private readonly ILogger<ReviewService> _logger;

    public ReviewService(ApplicationDbContext context, ICatalogService catalog, ILogger<ReviewService> logger)
    {
        _context = context;
        _catalog = catalog;
        _logger = logger;
    }

    public async Task<BookReviewsDto> GetForBookAsync(int bookId, string? userId, CancellationToken cancellationToken = default)
    {
        var rows = await _context.BookReviews
            .AsNoTracking()
            .Where(r => r.BookId == bookId)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new { r.Id, r.UserId, r.ReviewerName, r.Rating, r.Comment, r.CreatedAt })
            .ToListAsync(cancellationToken);

        var reviews = rows
            .Select(r => new ReviewDto(
                r.Id,
                r.ReviewerName,
                r.Rating,
                r.Comment,
                r.CreatedAt,
                userId is not null && r.UserId == userId))
            .ToList();

        var mine = reviews.FirstOrDefault(r => r.IsMine);
        var average = reviews.Count == 0 ? 0 : Math.Round(reviews.Average(r => r.Rating), 1);

        return new BookReviewsDto(average, reviews.Count, reviews, mine, userId is not null && mine is null);
    }

    public async Task<ReviewResult> AddAsync(
        int bookId, string userId, int rating, string? comment, CancellationToken cancellationToken = default)
    {
        if (rating is < 1 or > 5)
        {
            return ReviewResult.Fail("Choose a rating between 1 and 5 stars.");
        }

        if (!await _context.Books.AnyAsync(b => b.Id == bookId, cancellationToken))
        {
            return ReviewResult.Fail("That book could not be found.");
        }

        // Friendly path first; the unique index is the real guard when two tabs race.
        if (await _context.BookReviews.AnyAsync(r => r.BookId == bookId && r.UserId == userId, cancellationToken))
        {
            return ReviewResult.Fail("You already reviewed this book. Delete your review to write another one.");
        }

        var user = await _context.Users
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new { u.FirstName, u.LastName, u.Email })
            .FirstOrDefaultAsync(cancellationToken);

        var name = $"{user?.FirstName} {user?.LastName}".Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            name = string.IsNullOrWhiteSpace(user?.Email) ? "Reader" : user!.Email!.Split('@')[0];
        }

        var review = new BookReview
        {
            BookId = bookId,
            UserId = userId,
            Rating = rating,
            Comment = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim(),
            ReviewerName = name.Length > 100 ? name[..100] : name,
            CreatedAt = DateTime.UtcNow
        };

        _context.BookReviews.Add(review);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogInformation(ex, "Duplicate review for book {BookId} by {UserId}", bookId, userId);
            _context.Entry(review).State = EntityState.Detached;
            return ReviewResult.Fail("You already reviewed this book. Delete your review to write another one.");
        }

        _catalog.InvalidateHomeCache();
        return ReviewResult.Ok("Thank you — your review is live.");
    }

    public async Task<ReviewResult> DeleteAsync(int bookId, string userId, CancellationToken cancellationToken = default)
    {
        var review = await _context.BookReviews
            .FirstOrDefaultAsync(r => r.BookId == bookId && r.UserId == userId, cancellationToken);

        if (review is null)
        {
            return ReviewResult.Fail("You have not reviewed this book.");
        }

        _context.BookReviews.Remove(review);
        await _context.SaveChangesAsync(cancellationToken);
        _catalog.InvalidateHomeCache();

        return ReviewResult.Ok("Your review was deleted.");
    }
}
