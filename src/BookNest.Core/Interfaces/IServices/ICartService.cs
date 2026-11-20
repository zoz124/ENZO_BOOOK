using BookNest.Core.Models;

namespace BookNest.Core.Interfaces.IServices;

/// <summary>Resolves who owns the current cart: a signed-in user, or a guest session.</summary>
public interface ICartOwnerAccessor
{
    string? UserId { get; }
    string SessionId { get; }
    bool IsAuthenticated { get; }
}

public interface ICartService
{
    Task<CartSummary> GetAsync(CancellationToken cancellationToken = default);
    Task<CartOperationResult> AddAsync(int bookId, int quantity, CancellationToken cancellationToken = default);
    Task<CartOperationResult> UpdateAsync(int bookId, int quantity, CancellationToken cancellationToken = default);
    Task<CartOperationResult> RemoveAsync(int bookId, CancellationToken cancellationToken = default);
    Task MergeGuestCartAsync(string userId, CancellationToken cancellationToken = default);
    Task ClearAsync(CancellationToken cancellationToken = default);
}

public interface IWishlistService
{
    Task<IReadOnlyList<WishlistItemDto>> GetAsync(string userId, CancellationToken cancellationToken = default);
    Task<bool> AddAsync(string userId, int bookId, CancellationToken cancellationToken = default);
    Task<bool> RemoveAsync(string userId, int bookId, CancellationToken cancellationToken = default);
    Task<bool> ContainsAsync(string userId, int bookId, CancellationToken cancellationToken = default);

    /// <summary>Ids only, so catalogue cards can render the filled heart without a query each.</summary>
    Task<IReadOnlyList<int>> GetIdsAsync(string userId, CancellationToken cancellationToken = default);
}
