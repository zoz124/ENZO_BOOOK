using System.Security.Claims;
using BookNest.Core.Enums;
using BookNest.Core.Interfaces.IServices;
using BookNest.Core.Models;
using BookNest.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookNest.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class OrdersController : Controller
{
    private readonly IOrderService _orders;

    public OrdersController(IOrderService orders) => _orders = orders;

    private string AdminId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    public async Task<IActionResult> Index(OrderStatus? status, string? q, int page = 1, CancellationToken cancellationToken = default)
    {
        ViewData["Status"] = status;
        ViewData["Search"] = q;

        var model = new AdminOrdersViewModel
        {
            Status = status,
            Search = q,
            Orders = await _orders.GetOrdersAsync(status, q, page, 25, cancellationToken)
        };

        return View(model);
    }

    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var order = await _orders.GetOrderAsync(id, cancellationToken);
        if (order is null)
        {
            return NotFound();
        }

        return View(new OrderDetailsViewModel
        {
            Order = order,
            IsAdmin = true,
            AllowedNext = _orders.AllowedNextStatuses(order.Status)
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeStatus(int id, OrderStatus newStatus, string? note, CancellationToken cancellationToken)
    {
        var result = await _orders.ChangeStatusAsync(id, newStatus, AdminId, note, cancellationToken);
        TempData[result.Succeeded ? "Success" : "Error"] = result.Succeeded
            ? $"Status changed to {newStatus}."
            : result.Error;

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id, string reason, CancellationToken cancellationToken)
    {
        var result = await _orders.AdminCancelAsync(id, AdminId, reason ?? "Cancelled by an administrator", cancellationToken);
        TempData[result.Succeeded ? "Success" : "Error"] = result.Succeeded ? "Order cancelled." : result.Error;

        return RedirectToAction(nameof(Details), new { id });
    }
}

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class UsersController : Controller
{
    private readonly IAdminUserService _users;

    public UsersController(IAdminUserService users) => _users = users;

    private string AdminId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
        => View(await _users.GetAllAsync(cancellationToken));

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(string id, bool isActive, CancellationToken cancellationToken)
    {
        var ok = await _users.SetActiveAsync(id, isActive, AdminId, cancellationToken);
        TempData[ok ? "Success" : "Error"] = ok
            ? (isActive ? "Account enabled." : "Account disabled.")
            : "You cannot disable your own account (BR-09).";

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetRole(string id, string role, CancellationToken cancellationToken)
    {
        var ok = await _users.SetRoleAsync(id, role, AdminId, cancellationToken);
        TempData[ok ? "Success" : "Error"] = ok ? "Role updated." : "You cannot remove the Admin role from your own account (BR-09).";

        return RedirectToAction(nameof(Index));
    }
}
