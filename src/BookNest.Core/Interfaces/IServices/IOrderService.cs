using BookNest.Core.Enums;
using BookNest.Core.Models;

namespace BookNest.Core.Interfaces.IServices;

public interface IOrderService
{
    Task<OrderResult> PlaceOrderAsync(string userId, CheckoutRequest request, CancellationToken cancellationToken = default);
    Task<OrderResult> CancelOrderAsync(int orderId, string userId, string reason, CancellationToken cancellationToken = default);
    Task<OrderResult> AdminCancelAsync(int orderId, string adminUserId, string reason, CancellationToken cancellationToken = default);
    Task<OrderResult> ChangeStatusAsync(int orderId, OrderStatus newStatus, string changedByUserId, string? note = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OrderSummaryDto>> GetMyOrdersAsync(string userId, CancellationToken cancellationToken = default);
    Task<PagedResult<OrderSummaryDto>> GetOrdersAsync(OrderStatus? status, string? search, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<OrderDetailsDto?> GetOrderAsync(int orderId, CancellationToken cancellationToken = default);
    Task<OrderDetailsDto?> GetMyOrderAsync(int orderId, string userId, CancellationToken cancellationToken = default);
    IReadOnlyList<OrderStatus> AllowedNextStatuses(OrderStatus current);
}

public interface IAddressService
{
    Task<IReadOnlyList<AddressDto>> GetAsync(string userId, CancellationToken cancellationToken = default);
    Task<AddressDto?> GetAsync(string userId, int addressId, CancellationToken cancellationToken = default);
    Task<int> AddAsync(string userId, AddressDto address, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(string userId, int addressId, AddressDto address, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(string userId, int addressId, CancellationToken cancellationToken = default);
    Task<bool> SetDefaultAsync(string userId, int addressId, CancellationToken cancellationToken = default);
}

public interface IProfileService
{
    Task<AdminUserDto?> GetAsync(string userId, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(string userId, string fullName, string? phone, CancellationToken cancellationToken = default);
}

public interface IAdminDashboardService
{
    Task<DashboardStatsDto> GetStatsAsync(CancellationToken cancellationToken = default);
}

public interface IAdminUserService
{
    Task<IReadOnlyList<AdminUserDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<bool> SetActiveAsync(string userId, bool isActive, string actingAdminId, CancellationToken cancellationToken = default);
    Task<bool> SetRoleAsync(string userId, string role, string actingAdminId, CancellationToken cancellationToken = default);
}

/// <summary>Validated book-cover storage (NFR-12).</summary>
public interface IImageStorage
{
    Task<string?> SaveCoverAsync(Stream content, string fileName, CancellationToken cancellationToken = default);
    void Delete(string? publicUrl);
}
