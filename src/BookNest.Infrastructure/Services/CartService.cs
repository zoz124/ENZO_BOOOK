using BookNest.Core.Entities;
using BookNest.Core.Interfaces.IServices;
using BookNest.Core.Models;
using BookNest.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BookNest.Infrastructure.Services;

/// <summary>
/// Cart lives in the database for both guests (SessionId) and customers (UserId).
/// The guest cookie only carries an opaque, protected session id — never the cart.
/// </summary>
public class CartService : ICartService
{
    private readonly ApplicationDbContext _context;
    private readonly ICartOwnerAccessor _owner;
    private readonly ILogger<CartService> _logger;

    public CartService(ApplicationDbContext context, ICartOwnerAccessor owner, ILogger<CartService> logger)
    {
        _context = context;
        _owner = owner;
        _logger = logger;
    }

    private IQueryable<CartItem> OwnedCart()
    {
        if (_owner.IsAuthenticated && !string.IsNullOrEmpty(_owner.UserId))
        {
            var userId = _owner.UserId;
            return _context.CartItems.Where(c => c.UserId == userId);
        }

        var sessionId = _owner.SessionId;
        return _context.CartItems.Where(c => c.UserId == null && c.SessionId == sessionId);
    }

    public async Task<CartSummary> GetAsync(CancellationToken cancellationToken = default)
    {
        var lines = await OwnedCart()
            .AsNoTracking()
            .Where(c => !c.Book.IsDeleted)
            .OrderBy(c => c.BookId)
            .Select(c => new CartLine(
                c.BookId,
                c.Book.Title,
                c.Book.Author.Name,
                c.Book.CoverImageUrl,
                c.Book.DiscountPrice ?? c.Book.Price,
                c.Quantity,
                c.Book.Stock,
                c.Book.Stock >= c.Quantity))
            .ToListAsync(cancellationToken);

        var subtotal = lines.Sum(l => l.LineTotal);
        var shipping = lines.Count == 0 ? 0m : Core.Constants.StoreSettings.ShippingFor(subtotal);

        return new CartSummary(lines, subtotal, shipping, lines.Sum(l => l.Quantity));
    }

    public async Task<CartOperationResult> AddAsync(int bookId, int quantity, CancellationToken cancellationToken = default)
    {
        if (quantity < 1)
        {
            return CartOperationResult.Fail("Quantity must be at least 1.");
        }

        var book = await _context.Books
            .Where(b => b.Id == bookId)
            .Select(b => new { b.Id, b.Title, b.Stock })
            .FirstOrDefaultAsync(cancellationToken);

        if (book is null)
        {
            return CartOperationResult.Fail("That book could not be found.");
        }

        if (book.Stock <= 0)
        {
            return CartOperationResult.Fail($"“{book.Title}” is out of stock right now.");
        }

        var existing = await OwnedCart().FirstOrDefaultAsync(c => c.BookId == bookId, cancellationToken);
        var requested = quantity + (existing?.Quantity ?? 0);
        var allowed = Math.Min(requested, book.Stock);

        if (existing is null)
        {
            _context.CartItems.Add(new CartItem
            {
                BookId = bookId,
                Quantity = allowed,
                UserId = _owner.IsAuthenticated ? _owner.UserId : null,
                SessionId = _owner.IsAuthenticated ? null : _owner.SessionId
            });
        }
        else
        {
            existing.Quantity = allowed;
            existing.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(cancellationToken);

        var message = allowed < requested
            ? $"Only {allowed} copies of “{book.Title}” are available, so we added {allowed}."
            : $"“{book.Title}” was added to your bag.";

        return CartOperationResult.Ok(await CountAsync(cancellationToken), message);
    }

    public async Task<CartOperationResult> UpdateAsync(int bookId, int quantity, CancellationToken cancellationToken = default)
    {
        var item = await OwnedCart().FirstOrDefaultAsync(c => c.BookId == bookId, cancellationToken);
        if (item is null)
        {
            return CartOperationResult.Fail("That book is not in your bag.");
        }

        // A negative quantity is a tampered request, never a removal request.
        if (quantity < 0)
        {
            return CartOperationResult.Fail("Quantity cannot be negative.");
        }

        // Zero is the bag's explicit "remove this line" intent (the − button at one copy).
        if (quantity == 0)
        {
            _context.CartItems.Remove(item);
            await _context.SaveChangesAsync(cancellationToken);
            return CartOperationResult.Ok(await CountAsync(cancellationToken), "The book was removed from your bag.");
        }

        var stock = await _context.Books.Where(b => b.Id == bookId).Select(b => b.Stock).FirstOrDefaultAsync(cancellationToken);
        var allowed = Math.Min(quantity, Math.Max(stock, 0));

        if (allowed <= 0)
        {
            return CartOperationResult.Fail("That quantity is not available.");
        }

        item.Quantity = allowed;
        item.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        return CartOperationResult.Ok(
            await CountAsync(cancellationToken),
            allowed < quantity ? $"Only {allowed} copies are available." : null);
    }

    public async Task<CartOperationResult> RemoveAsync(int bookId, CancellationToken cancellationToken = default)
    {
        var item = await OwnedCart().FirstOrDefaultAsync(c => c.BookId == bookId, cancellationToken);
        if (item is null)
        {
            return CartOperationResult.Fail("That book is not in your bag.");
        }

        _context.CartItems.Remove(item);
        await _context.SaveChangesAsync(cancellationToken);

        return CartOperationResult.Ok(await CountAsync(cancellationToken), "The book was removed from your bag.");
    }

    public async Task MergeGuestCartAsync(string userId, CancellationToken cancellationToken = default)
    {
        var sessionId = _owner.SessionId;
        if (string.IsNullOrEmpty(sessionId))
        {
            return;
        }

        var guestItems = await _context.CartItems
            .Where(c => c.UserId == null && c.SessionId == sessionId)
            .ToListAsync(cancellationToken);

        if (guestItems.Count == 0)
        {
            return;
        }

        var userItems = await _context.CartItems
            .Where(c => c.UserId == userId)
            .ToDictionaryAsync(c => c.BookId, cancellationToken);

        var bookIds = guestItems.Select(g => g.BookId).ToList();
        var stocks = await _context.Books
            .Where(b => bookIds.Contains(b.Id))
            .Select(b => new { b.Id, b.Stock })
            .ToDictionaryAsync(b => b.Id, b => b.Stock, cancellationToken);

        foreach (var guest in guestItems)
        {
            var stock = stocks.GetValueOrDefault(guest.BookId);

            if (userItems.TryGetValue(guest.BookId, out var owned))
            {
                owned.Quantity = Math.Min(owned.Quantity + guest.Quantity, stock);
                owned.UpdatedAt = DateTime.UtcNow;
                _context.CartItems.Remove(guest);
            }
            else
            {
                guest.UserId = userId;
                guest.SessionId = null;
                guest.Quantity = Math.Min(guest.Quantity, stock);
            }
        }

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            // Two logins racing on the same session: the unique index wins, drop the duplicate.
            _logger.LogWarning(ex, "Cart merge conflict for user {UserId}; retrying once.", userId);

            foreach (var entry in _context.ChangeTracker.Entries<CartItem>().ToList())
            {
                entry.State = EntityState.Detached;
            }

            var leftovers = await _context.CartItems
                .Where(c => c.UserId == null && c.SessionId == sessionId)
                .ToListAsync(cancellationToken);

            _context.CartItems.RemoveRange(leftovers);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task ClearAsync(CancellationToken cancellationToken = default)
    {
        var items = await OwnedCart().ToListAsync(cancellationToken);
        if (items.Count == 0)
        {
            return;
        }

        _context.CartItems.RemoveRange(items);
        await _context.SaveChangesAsync(cancellationToken);
    }

    private Task<int> CountAsync(CancellationToken cancellationToken)
        => OwnedCart().SumAsync(c => (int?)c.Quantity, cancellationToken).ContinueWith(t => t.Result ?? 0, cancellationToken);
}
