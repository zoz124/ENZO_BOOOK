using BookNest.Core.Models;

namespace BookNest.Core.Interfaces.IServices;

/// <summary>
/// Ratings &amp; comments: one review per customer per book, deletable by its author.
/// The book's rating is always the average of the live reviews, never a stored field.
/// </summary>
public interface IReviewService
{
    Task<BookReviewsDto> GetForBookAsync(int bookId, string? userId, CancellationToken cancellationToken = default);
    Task<ReviewResult> AddAsync(int bookId, string userId, int rating, string? comment, CancellationToken cancellationToken = default);
    Task<ReviewResult> DeleteAsync(int bookId, string userId, CancellationToken cancellationToken = default);
}
