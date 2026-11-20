using BookNest.Core.Constants;
using BookNest.Core.Entities;
using BookNest.Core.Enums;
using BookNest.Core.Interfaces.IServices;
using BookNest.Core.Models;
using BookNest.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BookNest.Infrastructure.Services;

public class OrderService : IOrderService
{
    private static readonly Dictionary<OrderStatus, OrderStatus[]> Transitions = new()
    {
        [OrderStatus.Pending] = new[] { OrderStatus.Processing, OrderStatus.Cancelled },
        [OrderStatus.Processing] = new[] { OrderStatus.Shipped, OrderStatus.Cancelled },
        [OrderStatus.Shipped] = new[] { OrderStatus.Delivered },
        [OrderStatus.Delivered] = Array.Empty<OrderStatus>(),
        [OrderStatus.Cancelled] = Array.Empty<OrderStatus>()
    };

    private readonly ApplicationDbContext _context;
    private readonly ILogger<OrderService> _logger;

    public OrderService(ApplicationDbContext context, ILogger<OrderService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public IReadOnlyList<OrderStatus> AllowedNextStatuses(OrderStatus current)
        => Transitions.TryGetValue(current, out var next) ? next : Array.Empty<OrderStatus>();

    public Task<OrderResult> PlaceOrderAsync(string userId, CheckoutRequest request, CancellationToken cancellationToken = default)
    {
        // A retrying execution strategy cannot wrap a raw BeginTransaction; running the
        // whole unit through the strategy keeps both retries and the transaction valid.
        var strategy = _context.Database.CreateExecutionStrategy();
        return strategy.ExecuteAsync(() => PlaceOrderCoreAsync(userId, request, cancellationToken));
    }

    private async Task<OrderResult> PlaceOrderCoreAsync(string userId, CheckoutRequest request, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(request.IdempotencyKey))
        {
            var existing = await _context.Orders
                .Where(o => o.IdempotencyKey == request.IdempotencyKey && o.UserId == userId)
                .Select(o => (int?)o.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (existing is not null)
            {
                _logger.LogInformation("Idempotent re-submit for key {Key} returned order {OrderId}",
                    request.IdempotencyKey, existing.Value);
                return OrderResult.Success(existing.Value);
            }
        }

        var cartItems = await _context.CartItems
            .Include(c => c.Book)
            .Where(c => c.UserId == userId)
            .ToListAsync(cancellationToken);

        if (cartItems.Count == 0)
        {
            // A simultaneous submit may have already placed this order and emptied
            // the bag; the key lets us answer with that order instead of an error.
            if (!string.IsNullOrWhiteSpace(request.IdempotencyKey))
            {
                var placed = await _context.Orders
                    .AsNoTracking()
                    .Where(o => o.IdempotencyKey == request.IdempotencyKey && o.UserId == userId)
                    .Select(o => (int?)o.Id)
                    .FirstOrDefaultAsync(cancellationToken);

                if (placed is not null)
                {
                    return OrderResult.Success(placed.Value);
                }
            }

            return OrderResult.Failure("Your bag is empty.");
        }

        // Sort by BookId so concurrent orders always take row locks in the same order.
        var sorted = cartItems.OrderBy(c => c.BookId).ToList();

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var shipping = await ResolveShippingAsync(userId, request, cancellationToken);
            if (shipping is null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return OrderResult.Failure("Shipping details are incomplete.");
            }

            var subtotal = sorted.Sum(i => (i.Book.DiscountPrice ?? i.Book.Price) * i.Quantity);
            var shippingCost = StoreSettings.ShippingFor(subtotal);

            var order = new Order
            {
                OrderNumber = GenerateOrderNumber(),
                IdempotencyKey = string.IsNullOrWhiteSpace(request.IdempotencyKey) ? null : request.IdempotencyKey,
                UserId = userId,
                Status = OrderStatus.Pending,
                Subtotal = subtotal,
                ShippingCost = shippingCost,
                Total = subtotal + shippingCost,
                ShippingName = shipping.Value.Name,
                ShippingPhone = shipping.Value.Phone,
                ShippingCity = shipping.Value.City,
                ShippingAddress = shipping.Value.Address,
                ShippingPostalCode = shipping.Value.PostalCode,
                PaymentMethod = NormalizePaymentMethod(request.PaymentMethod),
                CreatedAt = DateTime.UtcNow
            };

            _context.Orders.Add(order);

            try
            {
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException) when (!string.IsNullOrWhiteSpace(request.IdempotencyKey))
            {
                // A simultaneous submit with the same key inserted first. Treat this
                // submit as the same order instead of showing a generic failure.
                await transaction.RollbackAsync(cancellationToken);
                _context.Entry(order).State = EntityState.Detached;

                var winnerId = await _context.Orders
                    .AsNoTracking()
                    .Where(o => o.IdempotencyKey == request.IdempotencyKey && o.UserId == userId)
                    .Select(o => (int?)o.Id)
                    .FirstOrDefaultAsync(cancellationToken);

                if (winnerId is not null)
                {
                    _logger.LogInformation("Concurrent duplicate submit for key {Key} resolved to order {OrderId}",
                        request.IdempotencyKey, winnerId.Value);
                    return OrderResult.Success(winnerId.Value);
                }

                throw;
            }

            foreach (var item in sorted)
            {
                var unitPrice = item.Book.DiscountPrice ?? item.Book.Price;

                // Atomic, conditional decrement: the WHERE clause is what prevents overselling
                // under concurrency — reading Stock first and trusting it would race.
                var rowsAffected = await _context.Books
                    .Where(b => b.Id == item.BookId && !b.IsDeleted && b.Stock >= item.Quantity)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(b => b.Stock, b => b.Stock - item.Quantity)
                        .SetProperty(b => b.UpdatedAt, DateTime.UtcNow), cancellationToken);

                if (rowsAffected == 0)
                {
                    await transaction.RollbackAsync(cancellationToken);

                    var available = await _context.Books
                        .Where(b => b.Id == item.BookId)
                        .Select(b => b.Stock)
                        .FirstOrDefaultAsync(cancellationToken);

                    _logger.LogWarning("Insufficient stock for book {BookId} (requested {Requested}, available {Available})",
                        item.BookId, item.Quantity, available);

                    return OrderResult.Failure(
                        $"“{item.Book.Title}” is not available in the requested quantity. Available: {available}");
                }

                _context.OrderItems.Add(new OrderItem
                {
                    OrderId = order.Id,
                    BookId = item.BookId,
                    BookTitle = item.Book.Title,
                    BookISBN = item.Book.ISBN,
                    UnitPrice = unitPrice,
                    Quantity = item.Quantity,
                    Subtotal = unitPrice * item.Quantity
                });
            }

            _context.OrderStatusHistories.Add(new OrderStatusHistory
            {
                OrderId = order.Id,
                FromStatus = OrderStatus.Pending,
                ToStatus = OrderStatus.Pending,
                Notes = "Order placed",
                ChangedAt = DateTime.UtcNow
            });

            _context.CartItems.RemoveRange(sorted);

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation("Order {OrderNumber} placed by {UserId} for {Total}", order.OrderNumber, userId, order.Total);
            return OrderResult.Success(order.Id);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Failed to place order for user {UserId}", userId);
            return OrderResult.Failure("Something went wrong while placing your order. Please try again.");
        }
    }

    private async Task<(string Name, string Phone, string City, string Address, string? PostalCode)?> ResolveShippingAsync(
        string userId, CheckoutRequest request, CancellationToken cancellationToken)
    {
        if (request.AddressId is > 0)
        {
            var address = await _context.Addresses
                .Where(a => a.Id == request.AddressId && a.UserId == userId)
                .FirstOrDefaultAsync(cancellationToken);

            if (address is null)
            {
                return null;
            }

            return (address.FullName, address.Phone, address.City, address.AddressLine, address.PostalCode);
        }

        if (string.IsNullOrWhiteSpace(request.FullName) ||
            string.IsNullOrWhiteSpace(request.Phone) ||
            string.IsNullOrWhiteSpace(request.City) ||
            string.IsNullOrWhiteSpace(request.AddressLine))
        {
            return null;
        }

        if (request.SaveAddress)
        {
            var isFirst = !await _context.Addresses.AnyAsync(a => a.UserId == userId, cancellationToken);

            _context.Addresses.Add(new Address
            {
                UserId = userId,
                FullName = request.FullName.Trim(),
                Phone = request.Phone.Trim(),
                City = request.City.Trim(),
                AddressLine = request.AddressLine.Trim(),
                PostalCode = request.PostalCode,
                IsDefault = isFirst
            });
        }

        return (request.FullName.Trim(), request.Phone.Trim(), request.City.Trim(), request.AddressLine.Trim(), request.PostalCode);
    }

    /// <summary>Only two methods exist in the store: cash on delivery and Visa/card.</summary>
    private static string NormalizePaymentMethod(string? value)
        => string.Equals(value?.Trim(), "Visa", StringComparison.OrdinalIgnoreCase) ||
           string.Equals(value?.Trim(), "Card", StringComparison.OrdinalIgnoreCase)
            ? "Visa"
            : "Cash";

    private static string GenerateOrderNumber()
        => $"BN-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";

    public Task<OrderResult> CancelOrderAsync(int orderId, string userId, string reason, CancellationToken cancellationToken = default)
        => SetStatusInternalAsync(orderId, OrderStatus.Cancelled, userId, reason, ownerUserId: userId, cancellationToken);

    public Task<OrderResult> AdminCancelAsync(int orderId, string adminUserId, string reason, CancellationToken cancellationToken = default)
        => SetStatusInternalAsync(orderId, OrderStatus.Cancelled, adminUserId, reason, ownerUserId: null, cancellationToken);

    public Task<OrderResult> ChangeStatusAsync(int orderId, OrderStatus newStatus, string changedByUserId, string? note = null, CancellationToken cancellationToken = default)
        => SetStatusInternalAsync(orderId, newStatus, changedByUserId, note, ownerUserId: null, cancellationToken);

    private Task<OrderResult> SetStatusInternalAsync(
        int orderId,
        OrderStatus newStatus,
        string changedByUserId,
        string? note,
        string? ownerUserId,
        CancellationToken cancellationToken)
    {
        var strategy = _context.Database.CreateExecutionStrategy();
        return strategy.ExecuteAsync(() =>
            SetStatusCoreAsync(orderId, newStatus, changedByUserId, note, ownerUserId, cancellationToken));
    }

    private async Task<OrderResult> SetStatusCoreAsync(
        int orderId,
        OrderStatus newStatus,
        string changedByUserId,
        string? note,
        string? ownerUserId,
        CancellationToken cancellationToken)
    {
        var order = await _context.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);

        if (order is null || (ownerUserId is not null && order.UserId != ownerUserId))
        {
            return OrderResult.Failure("Order not found.");
        }

        if (!AllowedNextStatuses(order.Status).Contains(newStatus))
        {
            return OrderResult.Failure($"An order cannot move from {order.Status} to {newStatus}.");
        }

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            if (newStatus == OrderStatus.Cancelled)
            {
                foreach (var item in order.Items.OrderBy(i => i.BookId))
                {
                    var bookId = item.BookId;
                    var quantity = item.Quantity;

                    await _context.Books
                        .Where(b => b.Id == bookId)
                        .ExecuteUpdateAsync(setters => setters
                            .SetProperty(b => b.Stock, b => b.Stock + quantity)
                            .SetProperty(b => b.UpdatedAt, DateTime.UtcNow), cancellationToken);
                }

                order.CancelledAt = DateTime.UtcNow;
                order.CancellationReason = note;
            }

            if (newStatus == OrderStatus.Shipped)
            {
                order.ShippedAt = DateTime.UtcNow;
            }

            if (newStatus == OrderStatus.Delivered)
            {
                order.DeliveredAt = DateTime.UtcNow;
            }

            _context.OrderStatusHistories.Add(new OrderStatusHistory
            {
                OrderId = order.Id,
                FromStatus = order.Status,
                ToStatus = newStatus,
                Notes = note,
                ChangedByUserId = changedByUserId,
                ChangedAt = DateTime.UtcNow
            });

            var previous = order.Status;
            order.Status = newStatus;
            order.UpdatedAt = DateTime.UtcNow;

            // RowVersion makes two admins changing the same order a concurrency conflict, not a lost update.
            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation("Order {OrderId} moved {From} → {To} by {User}", orderId, previous, newStatus, changedByUserId);
            return OrderResult.Success(order.Id);
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync(cancellationToken);
            return OrderResult.Failure("Someone else updated this order. Refresh the page and try again.");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Failed to change status of order {OrderId}", orderId);
            return OrderResult.Failure("Something went wrong while updating the order.");
        }
    }

    public async Task<IReadOnlyList<OrderSummaryDto>> GetMyOrdersAsync(string userId, CancellationToken cancellationToken = default)
    {
        var user = await _context.Users.Where(u => u.Id == userId)
            .Select(u => new { Name = (u.FirstName ?? "") + " " + (u.LastName ?? "") })
            .FirstOrDefaultAsync(cancellationToken);

        var name = string.IsNullOrWhiteSpace(user?.Name) ? "Customer" : user!.Name.Trim();

        return await _context.Orders
            .AsNoTracking()
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.CreatedAt)
            .Select(o => new OrderSummaryDto(
                o.Id,
                o.OrderNumber,
                o.CreatedAt,
                o.Status,
                o.Total,
                o.Items.Sum(i => i.Quantity),
                name))
            .ToListAsync(cancellationToken);
    }

    public async Task<PagedResult<OrderSummaryDto>> GetOrdersAsync(
        OrderStatus? status, string? search, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _context.Orders.AsNoTracking();

        if (status.HasValue)
        {
            query = query.Where(o => o.Status == status.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(o => EF.Functions.Like(o.OrderNumber, $"%{term}%") ||
                                     _context.Users.Any(u => u.Id == o.UserId && u.Email != null && EF.Functions.Like(u.Email, $"%{term}%")));
        }

        var total = await query.CountAsync(cancellationToken);
        var safePageSize = pageSize is > 0 and <= 100 ? pageSize : 25;
        var safePage = Math.Max(1, page);

        var items = await query
            .OrderByDescending(o => o.CreatedAt)
            .Skip((safePage - 1) * safePageSize)
            .Take(safePageSize)
            .Select(o => new OrderSummaryDto(
                o.Id,
                o.OrderNumber,
                o.CreatedAt,
                o.Status,
                o.Total,
                o.Items.Sum(i => i.Quantity),
                _context.Users.Where(u => u.Id == o.UserId)
                    .Select(u => ((u.FirstName ?? "") + " " + (u.LastName ?? ""))).FirstOrDefault() ?? "Customer"))
            .ToListAsync(cancellationToken);

        return new PagedResult<OrderSummaryDto>(items, safePage, safePageSize, total);
    }

    public async Task<OrderDetailsDto?> GetOrderAsync(int orderId, CancellationToken cancellationToken = default)
    {
        var order = await _context.Orders
            .AsNoTracking()
            .Where(o => o.Id == orderId)
            .Select(o => new
            {
                Order = o,
                CustomerName = _context.Users.Where(u => u.Id == o.UserId)
                    .Select(u => ((u.FirstName ?? "") + " " + (u.LastName ?? ""))).FirstOrDefault(),
                CustomerEmail = _context.Users.Where(u => u.Id == o.UserId).Select(u => u.Email).FirstOrDefault()
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (order is null)
        {
            return null;
        }

        var lines = await _context.OrderItems
            .AsNoTracking()
            .Where(i => i.OrderId == orderId)
            .Select(i => new OrderLineDto(i.BookTitle, i.BookISBN, i.UnitPrice, i.Quantity, i.Subtotal))
            .ToListAsync(cancellationToken);

        var history = await _context.OrderStatusHistories
            .AsNoTracking()
            .Where(h => h.OrderId == orderId)
            .OrderBy(h => h.ChangedAt)
            .Select(h => new OrderStatusChangeDto(h.FromStatus, h.ToStatus, h.ChangedAt, h.ChangedByUserId, h.Notes))
            .ToListAsync(cancellationToken);

        var o = order.Order;

        return new OrderDetailsDto(
            o.Id, o.OrderNumber, o.CreatedAt, o.Status, o.Subtotal, o.ShippingCost, o.Total,
            o.ShippingName, o.ShippingPhone, o.ShippingCity, o.ShippingAddress, o.ShippingPostalCode,
            o.PaymentMethod,
            string.IsNullOrWhiteSpace(order.CustomerName) ? "Customer" : order.CustomerName.Trim(),
            order.CustomerEmail ?? string.Empty,
            lines, history);
    }

    public async Task<OrderDetailsDto?> GetMyOrderAsync(int orderId, string userId, CancellationToken cancellationToken = default)
    {
        // IDOR guard: scope by owner first, then reuse the projection.
        var owns = await _context.Orders.AnyAsync(o => o.Id == orderId && o.UserId == userId, cancellationToken);
        return owns ? await GetOrderAsync(orderId, cancellationToken) : null;
    }
}
