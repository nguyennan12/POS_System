using Microsoft.Extensions.Options;
using POS.Api.Options;

namespace POS.Api.Middleware;

public sealed class PaymentHubOriginMiddleware(RequestDelegate next, IOptions<PaymentHubOptions> options)
{
    public Task InvokeAsync(HttpContext context)
    {
        // CORS does not protect WebSocket handshakes. Native clients normally send no Origin.
        if (context.Request.Path.StartsWithSegments("/hubs") && context.Request.Headers.TryGetValue("Origin", out var origin))
        {
            var sameOrigin = $"{context.Request.Scheme}://{context.Request.Host}";
            if (origin.Count != 1 || (!string.Equals(origin[0], sameOrigin, StringComparison.OrdinalIgnoreCase) &&
                !options.Value.AllowedOrigins.Contains(origin[0], StringComparer.OrdinalIgnoreCase)))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return Task.CompletedTask;
            }
        }
        return next(context);
    }
}
