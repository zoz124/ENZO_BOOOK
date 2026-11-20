using System.Security.Claims;
using BookNest.Core.Interfaces.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookNest.Web.Controllers;

public class CartController : Controller
{
    private readonly ICartService _cart;

    public CartController(ICartService cart) => _cart = cart;

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
        => View(await _cart.GetAsync(cancellationToken));

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(int bookId, int quantity, string? returnUrl, CancellationToken cancellationToken)
    {
        var result = await _cart.AddAsync(bookId, quantity, cancellationToken);
        TempData[result.Succeeded ? "Success" : "Error"] = result.Message;

        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        {
            return Json(new { result.Succeeded, result.Message, result.TotalItems });
        }

        return Redirect(string.IsNullOrWhiteSpace(returnUrl) || !Url.IsLocalUrl(returnUrl) ? Url.Action(nameof(Index))! : returnUrl);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(int bookId, int quantity, CancellationToken cancellationToken)
    {
        var result = await _cart.UpdateAsync(bookId, quantity, cancellationToken);
        TempData[result.Succeeded ? "Success" : "Error"] = result.Message;

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Remove(int bookId, CancellationToken cancellationToken)
    {
        var result = await _cart.RemoveAsync(bookId, cancellationToken);
        TempData[result.Succeeded ? "Success" : "Error"] = result.Message;

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Count(CancellationToken cancellationToken)
    {
        var cart = await _cart.GetAsync(cancellationToken);
        return Json(new { totalItems = cart.TotalItems, total = cart.Total });
    }
}

[Authorize]
public class WishlistController : Controller
{
    private readonly IWishlistService _wishlist;

    public WishlistController(IWishlistService wishlist) => _wishlist = wishlist;

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
        => View(await _wishlist.GetAsync(UserId, cancellationToken));

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(int bookId, string? returnUrl, CancellationToken cancellationToken)
    {
        await _wishlist.AddAsync(UserId, bookId, cancellationToken);
        TempData["Success"] = "Added to your wishlist.";

        return Redirect(string.IsNullOrWhiteSpace(returnUrl) || !Url.IsLocalUrl(returnUrl)
            ? Url.Action(nameof(Index))!
            : returnUrl);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Remove(int bookId, CancellationToken cancellationToken)
    {
        await _wishlist.RemoveAsync(UserId, bookId, cancellationToken);
        TempData["Success"] = "Removed from your wishlist.";

        return RedirectToAction(nameof(Index));
    }
}
