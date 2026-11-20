namespace BookNest.Web.Infrastructure;

/// <summary>T-05: baseline security response headers.</summary>
public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(() =>
        {
            var headers = context.Response.Headers;

            headers["X-Content-Type-Options"] = "nosniff";
            headers["X-Frame-Options"] = "DENY";
            headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
            headers["Permissions-Policy"] = "geolocation=(), microphone=(), camera=()";
            headers["Cross-Origin-Opener-Policy"] = "same-origin";

            // Inline styles are used by a few components; scripts are all local files.
            headers["Content-Security-Policy"] =
                // Fonts are self-hosted (wwwroot/fonts); covers come from the Unsplash CDN.
                "default-src 'self'; img-src 'self' data: https://images.unsplash.com; " +
                "style-src 'self' 'unsafe-inline'; script-src 'self'; font-src 'self'; " +
                "frame-ancestors 'none'; base-uri 'self'; form-action 'self'";

            return Task.CompletedTask;
        });

        await _next(context);
    }
}
