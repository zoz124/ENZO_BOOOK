using System.Security.Claims;
using BookNest.Core.Interfaces.IServices;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;

namespace BookNest.Web.Controllers;

public class HomeController : Controller
{
    private readonly ICatalogService _catalog;
    private readonly IWishlistService _wishlist;

    public HomeController(ICatalogService catalog, IWishlistService wishlist)
    {
        _catalog = catalog;
        _wishlist = wishlist;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId is not null)
            {
                ViewData["WishlistIds"] = await _wishlist.GetIdsAsync(userId, cancellationToken);
            }
        }

        return View(await _catalog.GetHomePageAsync(cancellationToken));
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() => View("Status", 500);

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Status(int code = 404)
    {
        ViewData["StatusCode"] = code;

        return code switch
        {
            403 => View("Status", 403),
            500 => View("Status", 500),
            _ => View("Status", 404)
        };
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult SetCulture(string culture, string? returnUrl)
    {
        var supported = new[] { "ar-EG", "en-US" };
        if (supported.Contains(culture))
        {
            Response.Cookies.Append(
                CookieRequestCultureProvider.DefaultCookieName,
                CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
                new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1), IsEssential = true });
        }

        return LocalRedirect(string.IsNullOrWhiteSpace(returnUrl) || !Url.IsLocalUrl(returnUrl) ? "/" : returnUrl);
    }
}
