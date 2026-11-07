using System.Diagnostics;

namespace POS.Api.Middleware;

public sealed class PaymentWebhookTimingMiddleware(RequestDelegate next)
{
    public static readonly object TimestampKey = new();

    public Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path.StartsWithSegments("/api/v1/payments/webhook"))
            context.Items[TimestampKey] = Stopwatch.GetTimestamp();
        return next(context);
    }
}
