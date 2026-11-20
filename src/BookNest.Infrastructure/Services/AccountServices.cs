using BookNest.Core.Entities;
using BookNest.Core.Interfaces.IServices;
using BookNest.Core.Models;
using BookNest.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BookNest.Infrastructure.Services;

public class WishlistService : IWishlistService
{
    private readonly ApplicationDbContext _context;

    public WishlistService(ApplicationDbContext context) => _context = context;

    public async Task<IReadOnlyList<WishlistItemDto>> GetAsync(string userId, CancellationToken cancellationToken = default)
        => await _context.WishlistItems
            .AsNoTracking()
            .Where(w => w.UserId == userId)
            .OrderByDescending(w => w.CreatedAt)
            .Select(w => new WishlistItemDto(
                w.BookId,
                w.Book.Title,
                w.Book.Author.Name,
                w.Book.Price,
                w.Book.DiscountPrice ?? w.Book.Price,
                w.Book.Stock,
                w.Book.CoverImageUrl))
            .ToListAsync(cancellationToken);

    public async Task<bool> AddAsync(string userId, int bookId, CancellationToken cancellationToken = default)
    {
        var exists = await _context.WishlistItems.AnyAsync(w => w.UserId == userId && w.BookId == bookId, cancellationToken);
        if (exists)
        {
            return true;
        }

        var bookExists = await _context.Books.AnyAsync(b => b.Id == bookId, cancellationToken);
        if (!bookExists)
        {
            return false;
        }

        _context.WishlistItems.Add(new WishlistItem { UserId = userId, BookId = bookId });

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException)
        {
            // Double-click on "add" hits the unique (UserId, BookId) index — that is fine.
            return true;
        }
    }

    public async Task<bool> RemoveAsync(string userId, int bookId, CancellationToken cancellationToken = default)
    {
        var item = await _context.WishlistItems
            .FirstOrDefaultAsync(w => w.UserId == userId && w.BookId == bookId, cancellationToken);

        if (item is null)
        {
            return false;
        }

        _context.WishlistItems.Remove(item);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public Task<bool> ContainsAsync(string userId, int bookId, CancellationToken cancellationToken = default)
        => _context.WishlistItems.AnyAsync(w => w.UserId == userId && w.BookId == bookId, cancellationToken);

    public async Task<IReadOnlyList<int>> GetIdsAsync(string userId, CancellationToken cancellationToken = default)
        => await _context.WishlistItems
            .AsNoTracking()
            .Where(w => w.UserId == userId)
            .Select(w => w.BookId)
            .ToListAsync(cancellationToken);
}

public class AddressService : IAddressService
{
    private readonly ApplicationDbContext _context;

    public AddressService(ApplicationDbContext context) => _context = context;

    public async Task<IReadOnlyList<AddressDto>> GetAsync(string userId, CancellationToken cancellationToken = default)
        => await _context.Addresses
            .AsNoTracking()
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.IsDefault)
            .ThenByDescending(a => a.Id)
            .Select(a => new AddressDto(a.Id, a.FullName, a.Phone, a.City, a.AddressLine, a.PostalCode, a.IsDefault))
            .ToListAsync(cancellationToken);

    public async Task<AddressDto?> GetAsync(string userId, int addressId, CancellationToken cancellationToken = default)
        => await _context.Addresses
            .AsNoTracking()
            .Where(a => a.Id == addressId && a.UserId == userId)
            .Select(a => new AddressDto(a.Id, a.FullName, a.Phone, a.City, a.AddressLine, a.PostalCode, a.IsDefault))
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<int> AddAsync(string userId, AddressDto address, CancellationToken cancellationToken = default)
    {
        var hasAny = await _context.Addresses.AnyAsync(a => a.UserId == userId, cancellationToken);
        var makeDefault = address.IsDefault || !hasAny;

        if (makeDefault)
        {
            await _context.Addresses
                .Where(a => a.UserId == userId && a.IsDefault)
                .ExecuteUpdateAsync(s => s.SetProperty(a => a.IsDefault, false), cancellationToken);
        }

        var entity = new Address
        {
            UserId = userId,
            FullName = address.FullName,
            Phone = address.Phone,
            City = address.City,
            AddressLine = address.AddressLine,
            PostalCode = address.PostalCode,
            IsDefault = makeDefault
        };

        _context.Addresses.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return entity.Id;
    }

    public async Task<bool> UpdateAsync(string userId, int addressId, AddressDto address, CancellationToken cancellationToken = default)
    {
        var entity = await _context.Addresses
            .FirstOrDefaultAsync(a => a.Id == addressId && a.UserId == userId, cancellationToken);

        if (entity is null)
        {
            return false;
        }

        entity.FullName = address.FullName;
        entity.Phone = address.Phone;
        entity.City = address.City;
        entity.AddressLine = address.AddressLine;
        entity.PostalCode = address.PostalCode;
        entity.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteAsync(string userId, int addressId, CancellationToken cancellationToken = default)
    {
        var entity = await _context.Addresses
            .FirstOrDefaultAsync(a => a.Id == addressId && a.UserId == userId, cancellationToken);

        if (entity is null)
        {
            return false;
        }

        _context.Addresses.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);

        if (entity.IsDefault)
        {
            var next = await _context.Addresses
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (next is not null)
            {
                next.IsDefault = true;
                await _context.SaveChangesAsync(cancellationToken);
            }
        }

        return true;
    }

    public async Task<bool> SetDefaultAsync(string userId, int addressId, CancellationToken cancellationToken = default)
    {
        var owns = await _context.Addresses.AnyAsync(a => a.Id == addressId && a.UserId == userId, cancellationToken);
        if (!owns)
        {
            return false;
        }

        await _context.Addresses
            .Where(a => a.UserId == userId)
            .ExecuteUpdateAsync(s => s.SetProperty(a => a.IsDefault, a => a.Id == addressId), cancellationToken);

        return true;
    }
}

public class ProfileService : IProfileService
{
    private readonly ApplicationDbContext _context;

    public ProfileService(ApplicationDbContext context) => _context = context;

    public async Task<AdminUserDto?> GetAsync(string userId, CancellationToken cancellationToken = default)
    {
        var user = await _context.Users
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new { u.Id, u.Email, u.FirstName, u.LastName, u.IsActive, u.CreatedAt })
            .FirstOrDefaultAsync(cancellationToken);

        if (user is null)
        {
            return null;
        }

        return new AdminUserDto(
            user.Id,
            user.Email ?? string.Empty,
            $"{user.FirstName} {user.LastName}".Trim(),
            user.IsActive,
            Array.Empty<string>(),
            user.CreatedAt);
    }

    public async Task<bool> UpdateAsync(string userId, string fullName, string? phone, CancellationToken cancellationToken = default)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null)
        {
            return false;
        }

        var parts = (fullName ?? string.Empty).Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        user.FirstName = parts.Length > 0 ? parts[0] : null;
        user.LastName = parts.Length > 1 ? parts[1] : null;
        user.PhoneNumber = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim();
        user.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}

public class AdminDashboardService : IAdminDashboardService
{
    private readonly ApplicationDbContext _context;

    public AdminDashboardService(ApplicationDbContext context) => _context = context;

    public async Task<DashboardStatsDto> GetStatsAsync(CancellationToken cancellationToken = default)
    {
        var billableOrders = _context.Orders.Where(o => o.Status != Core.Enums.OrderStatus.Cancelled);

        var totalSales = await billableOrders.SumAsync(o => (decimal?)o.Total, cancellationToken) ?? 0m;
        var ordersCount = await _context.Orders.CountAsync(cancellationToken);
        var newOrders = await _context.Orders.CountAsync(o => o.Status == Core.Enums.OrderStatus.Pending, cancellationToken);

        var lowStock = await _context.Books.AsNoTracking()
            .Where(b => b.Stock <= Core.Constants.StoreSettings.LowStockThreshold)
            .OrderBy(b => b.Stock)
            .Take(6)
            .Select(CatalogService.CardProjection)
            .ToListAsync(cancellationToken);

        var since = DateTime.UtcNow.Date.AddDays(-29);

        // EF Core cannot translate a DTO constructor inside a GroupBy projection, so the
        // aggregates are projected anonymously on the server and mapped in memory.
        var dailyRows = await billableOrders
            .Where(o => o.CreatedAt >= since)
            .GroupBy(o => o.CreatedAt.Date)
            .Select(g => new { Day = g.Key, Total = g.Sum(o => o.Total) })
            .OrderBy(x => x.Day)
            .ToListAsync(cancellationToken);

        var salesLast30Days = dailyRows
            .Select(r => new DailySalesDto(r.Day, r.Total))
            .ToList();

        var topRows = await _context.OrderItems
            .Where(i => i.Order.Status != Core.Enums.OrderStatus.Cancelled)
            .GroupBy(i => i.BookTitle)
            .Select(g => new { Title = g.Key, Units = g.Sum(i => i.Quantity), Revenue = g.Sum(i => i.Subtotal) })
            .OrderByDescending(x => x.Units)
            .Take(10)
            .ToListAsync(cancellationToken);

        var topBooks = topRows
            .Select(r => new TopBookDto(r.Title, r.Units, r.Revenue))
            .ToList();

        // T-72: which categories actually sell.
        var categoryRows = await _context.OrderItems
            .Where(i => i.Order.Status != Core.Enums.OrderStatus.Cancelled)
            .GroupBy(i => i.Book.Category.Name)
            .Select(g => new { Name = g.Key, Units = g.Sum(i => i.Quantity), Revenue = g.Sum(i => i.Subtotal) })
            .OrderByDescending(x => x.Revenue)
            .ToListAsync(cancellationToken);

        var categorySales = categoryRows
            .Select(r => new CategorySalesDto(r.Name, r.Units, r.Revenue))
            .ToList();

        var outOfStock = await _context.Books.AsNoTracking()
            .CountAsync(b => b.Stock <= 0, cancellationToken);

        var reviewsCount = await _context.BookReviews.CountAsync(cancellationToken);

        var averageRating = reviewsCount == 0
            ? 0
            : Math.Round(await _context.BookReviews.AverageAsync(r => (double)r.Rating, cancellationToken), 1);

        var recentReviews = await _context.BookReviews
            .AsNoTracking()
            .OrderByDescending(r => r.CreatedAt)
            .Take(5)
            .Select(r => new AdminReviewDto(r.Id, r.Book.Title, r.ReviewerName, r.Rating, r.Comment, r.CreatedAt))
            .ToListAsync(cancellationToken);

        return new DashboardStatsDto(
            totalSales, ordersCount, newOrders, lowStock, salesLast30Days, topBooks, categorySales,
            outOfStock, reviewsCount, averageRating, recentReviews);
    }
}

public class AdminUserService : IAdminUserService
{
    private readonly ApplicationDbContext _context;
    private readonly Microsoft.AspNetCore.Identity.UserManager<Identity.ApplicationUser> _userManager;
    private readonly ILogger<AdminUserService> _logger;

    public AdminUserService(
        ApplicationDbContext context,
        Microsoft.AspNetCore.Identity.UserManager<Identity.ApplicationUser> userManager,
        ILogger<AdminUserService> logger)
    {
        _context = context;
        _userManager = userManager;
        _logger = logger;
    }

    public async Task<IReadOnlyList<AdminUserDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var result = new List<AdminUserDto>();

        foreach (var user in _context.Users.AsNoTracking().OrderBy(u => u.Email).ToList())
        {
            var roles = await _userManager.GetRolesAsync(user).ConfigureAwait(false);
            result.Add(new AdminUserDto(
                user.Id,
                user.Email ?? string.Empty,
                $"{user.FirstName} {user.LastName}".Trim(),
                user.IsActive,
                roles.ToArray(),
                user.CreatedAt));
        }

        return result;
    }

    public async Task<bool> SetActiveAsync(string userId, bool isActive, string actingAdminId, CancellationToken cancellationToken = default)
    {
        // BR-09: an admin must not lock themselves out.
        if (string.Equals(userId, actingAdminId, StringComparison.Ordinal))
        {
            return false;
        }

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null)
        {
            return false;
        }

        user.IsActive = isActive;
        user.LockoutEnabled = !isActive;
        user.LockoutEnd = isActive ? null : DateTimeOffset.UtcNow.AddYears(100);
        user.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Admin {Admin} set IsActive={IsActive} for {User}", actingAdminId, isActive, userId);
        return true;
    }

    public async Task<bool> SetRoleAsync(string userId, string role, string actingAdminId, CancellationToken cancellationToken = default)
    {
        if (string.Equals(userId, actingAdminId, StringComparison.Ordinal) &&
            string.Equals(role, DatabaseSeeder.AdminRole, StringComparison.OrdinalIgnoreCase))
        {
            return false; // BR-09
        }

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null)
        {
            return false;
        }

        var roles = await _userManager.GetRolesAsync(user).ConfigureAwait(false);

        if (string.Equals(role, DatabaseSeeder.AdminRole, StringComparison.OrdinalIgnoreCase))
        {
            if (!roles.Contains(DatabaseSeeder.AdminRole))
            {
                await _userManager.AddToRoleAsync(user, DatabaseSeeder.AdminRole).ConfigureAwait(false);
            }
        }
        else if (roles.Contains(DatabaseSeeder.AdminRole))
        {
            await _userManager.RemoveFromRoleAsync(user, DatabaseSeeder.AdminRole).ConfigureAwait(false);
            if (!roles.Contains(DatabaseSeeder.CustomerRole))
            {
                await _userManager.AddToRoleAsync(user, DatabaseSeeder.CustomerRole).ConfigureAwait(false);
            }
        }

        _logger.LogInformation("Admin {Admin} changed role of {User} to {Role}", actingAdminId, userId, role);
        return true;
    }
}
