namespace BookNest.Core.Models;

public sealed record ReviewDto(
    int Id,
    string ReviewerName,
    int Rating,
    string? Comment,
    DateTime CreatedAt,
    bool IsMine);

public sealed record BookReviewsDto(
    double AverageRating,
    int ReviewCount,
    IReadOnlyList<ReviewDto> Reviews,
    ReviewDto? MyReview,
    bool CanReview)
{
    public bool HasReviews => ReviewCount > 0;
}

public sealed record ReviewResult(bool Succeeded, string Message)
{
    public static ReviewResult Ok(string message) => new(true, message);
    public static ReviewResult Fail(string message) => new(false, message);
}

public sealed record AdminReviewDto(
    int Id,
    string BookTitle,
    string ReviewerName,
    int Rating,
    string? Comment,
    DateTime CreatedAt);
