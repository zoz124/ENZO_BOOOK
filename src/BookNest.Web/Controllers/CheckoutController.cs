using System.Security.Claims;
using BookNest.Core.Interfaces.IServices;
using BookNest.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace BookNest.Web.Controllers;

[Authorize]
public class CheckoutController : Controller
{
    private readonly ICartService _cart;
    private readonly IAddressService _addresses;
    private readonly IOrderService _orders;

    public CheckoutController(ICartService cart, IAddressService addresses, IOrderService orders)
    {
        _cart = cart;
        _addresses = addresses;
        _orders = orders;
    }

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [HttpGet]
    public async Task<IActionResult> Index(string? paymentMethod, CancellationToken cancellationToken)
    {
        var cart = await _cart.GetAsync(cancellationToken);

        if (cart.IsEmpty)
        {
            TempData["Error"] = "Your bag is empty.";
            return RedirectToAction("Index", "Cart");
        }

        var addresses = await _addresses.GetAsync(UserId, cancellationToken);
        var defaultAddress = addresses.FirstOrDefault(a => a.IsDefault) ?? addresses.FirstOrDefault();

        var model = new CheckoutViewModel
        {
            Cart = cart,
            Addresses = addresses,
            Form = new CheckoutForm
            {
                AddressId = defaultAddress?.Id,
                FullName = defaultAddress?.FullName,
                Phone = defaultAddress?.Phone,
                City = defaultAddress?.City,
                AddressLine = defaultAddress?.AddressLine,
                PostalCode = defaultAddress?.PostalCode,
                PaymentMethod = string.Equals(paymentMethod, "Visa", StringComparison.OrdinalIgnoreCase) ? "Visa" : "Cash",
                IdempotencyKey = Guid.NewGuid().ToString("N")
            }
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Place(CheckoutForm form, CancellationToken cancellationToken)
    {
        var cart = await _cart.GetAsync(cancellationToken);

        if (cart.IsEmpty)
        {
            // A double submit arrives after the first order emptied the bag. The key
            // still resolves to that order so the buyer sees the confirmation again.
            if (!string.IsNullOrWhiteSpace(form.IdempotencyKey))
            {
                var replay = await _orders.PlaceOrderAsync(UserId, new Core.Models.CheckoutRequest
                {
                    IdempotencyKey = form.IdempotencyKey,
                    PaymentMethod = form.PaymentMethod
                }, cancellationToken);

                if (replay is { Succeeded: true, OrderId: int replayId })
                {
                    return RedirectToAction("Success", "Orders", new { id = replayId });
                }
            }

            TempData["Error"] = "Your bag is empty.";
            return RedirectToAction("Index", "Cart");
        }

        if (form.AddressId is null or 0)
        {
            if (string.IsNullOrWhiteSpace(form.FullName) ||
                string.IsNullOrWhiteSpace(form.Phone) ||
                string.IsNullOrWhiteSpace(form.City) ||
                string.IsNullOrWhiteSpace(form.AddressLine))
            {
                ModelState.AddModelError(string.Empty, "Choose a saved address or fill in the shipping details.");
            }
        }

        if (!ModelState.IsValid)
        {
            return View("Index", new CheckoutViewModel
            {
                Cart = cart,
                Addresses = await _addresses.GetAsync(UserId, cancellationToken),
                Form = form
            });
        }

        var request = new Core.Models.CheckoutRequest
        {
            AddressId = form.AddressId,
            FullName = form.FullName,
            Phone = form.Phone,
            City = form.City,
            AddressLine = form.AddressLine,
            PostalCode = form.PostalCode,
            SaveAddress = form.SaveAddress,
            PaymentMethod = form.PaymentMethod,
            IdempotencyKey = form.IdempotencyKey
        };

        var result = await _orders.PlaceOrderAsync(UserId, request, cancellationToken);

        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "We could not place this order.");
            return View("Index", new CheckoutViewModel
            {
                Cart = await _cart.GetAsync(cancellationToken),
                Addresses = await _addresses.GetAsync(UserId, cancellationToken),
                Form = form
            });
        }

        return RedirectToAction("Success", "Orders", new { id = result.OrderId });
    }
}

[Authorize]
public class OrdersController : Controller
{
    private readonly IOrderService _orders;

    public OrdersController(IOrderService orders) => _orders = orders;

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
        => View(await _orders.GetMyOrdersAsync(UserId, cancellationToken));

    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var order = await _orders.GetMyOrderAsync(id, UserId, cancellationToken);

        if (order is null)
        {
            return NotFound();
        }

        return View(new OrderDetailsViewModel
        {
            Order = order,
            AllowedNext = _orders.AllowedNextStatuses(order.Status)
        });
    }

    public async Task<IActionResult> Success(int id, CancellationToken cancellationToken)
    {
        var order = await _orders.GetMyOrderAsync(id, UserId, cancellationToken);
        return order is null ? NotFound() : View(order);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id, string reason, CancellationToken cancellationToken)
    {
        var result = await _orders.CancelOrderAsync(id, UserId, reason ?? "Cancelled by the customer", cancellationToken);
        TempData[result.Succeeded ? "Success" : "Error"] = result.Succeeded ? "The order was cancelled." : result.Error;

        return RedirectToAction(nameof(Details), new { id });
    }
}

[Authorize]
public class ProfileController : Controller
{
    private readonly IProfileService _profile;
    private readonly IAddressService _addresses;
    private readonly UserManager<BookNest.Infrastructure.Identity.ApplicationUser> _userManager;

    public ProfileController(
        IProfileService profile,
        IAddressService addresses,
        UserManager<BookNest.Infrastructure.Identity.ApplicationUser> userManager)
    {
        _profile = profile;
        _addresses = addresses;
        _userManager = userManager;
    }

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var profile = await _profile.GetAsync(UserId, cancellationToken);
        var user = await _userManager.GetUserAsync(User);

        return View(new ProfileViewModel
        {
            FullName = profile?.FullName ?? string.Empty,
            Phone = user?.PhoneNumber,
            Email = profile?.Email ?? string.Empty,
            Addresses = await _addresses.GetAsync(UserId, cancellationToken)
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(ProfileViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            model.Addresses = await _addresses.GetAsync(UserId, cancellationToken);
            return View("Index", model);
        }

        await _profile.UpdateAsync(UserId, model.FullName, model.Phone, cancellationToken);
        TempData["Success"] = "Your details were updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddAddress(AddressFormViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "The address details are incomplete.";
            return RedirectToAction(nameof(Index));
        }

        await _addresses.AddAsync(UserId, new Core.Models.AddressDto(
            0, model.FullName, model.Phone, model.City, model.AddressLine, model.PostalCode, model.IsDefault), cancellationToken);

        TempData["Success"] = "The address was added.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAddress(int id, CancellationToken cancellationToken)
    {
        await _addresses.DeleteAsync(UserId, id, cancellationToken);
        TempData["Success"] = "The address was deleted.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetDefaultAddress(int id, CancellationToken cancellationToken)
    {
        await _addresses.SetDefaultAsync(UserId, id, cancellationToken);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "The password details are incomplete.";
            return RedirectToAction(nameof(Index));
        }

        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
        TempData[result.Succeeded ? "Success" : "Error"] = result.Succeeded
            ? "Your password was changed."
            : string.Join("; ", result.Errors.Select(e => e.Description));

        return RedirectToAction(nameof(Index));
    }
}
