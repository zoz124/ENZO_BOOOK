using System.Security.Claims;
using BookNest.Core.Interfaces.IServices;
using Microsoft.AspNetCore.DataProtection;

namespace BookNest.Web.Infrastructure;

/// <summary>
/// Resolves the cart owner. Signed-in users are keyed by user id; guests get an
/// opaque, data-protected session id in a cookie (the cart itself stays in the DB).
/// </summary>
public class CartOwnerAccessor : ICartOwnerAccessor
{
    public const string CookieName = "bn_cart";
    public const string SessionItemKey = "bn_cart_session";

    private readonly IHttpContextAccessor _httpContextAccessor;

    public CartOwnerAccessor(IHttpContextAccessor httpContextAccessor)
        => _httpContextAccessor = httpContextAccessor;

    public string? UserId => _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);

    public bool IsAuthenticated => _httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated == true;

    public string SessionId =>
        _httpContextAccessor.HttpContext?.Items[SessionItemKey] as string ?? string.Empty;
}

/// <summary>Ensures every guest request carries a protected cart-session cookie.</summary>
public class GuestSessionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IDataProtector _protector;

    public GuestSessionMiddleware(RequestDelegate next, IDataProtectionProvider provider)
    {
        _next = next;
        _protector = provider.CreateProtector("BookNest.CartSession.v1");
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Cookies.TryGetValue(CartOwnerAccessor.CookieName, out var protectedValue) &&
            !string.IsNullOrEmpty(protectedValue))
        {
            try
            {
                context.Items[CartOwnerAccessor.SessionItemKey] = _protector.Unprotect(protectedValue);
                await _next(context);
                return;
            }
            catch (System.Security.Cryptography.CryptographicException)
            {
                // Tampered or rotated key: fall through and issue a fresh id.
            }
        }

        var sessionId = Guid.NewGuid().ToString("N");
        context.Items[CartOwnerAccessor.SessionItemKey] = sessionId;

        context.Response.Cookies.Append(
            CartOwnerAccessor.CookieName,
            _protector.Protect(sessionId),
            new CookieOptions
            {
                HttpOnly = true,
                IsEssential = true,
                SameSite = SameSiteMode.Lax,
                Secure = context.Request.IsHttps,
                Expires = DateTimeOffset.UtcNow.AddDays(14)
            });

        await _next(context);
    }
}
