using System.Diagnostics;
using Microsoft.AspNetCore.SignalR;
using POS.Api.Hubs;
using POS.Api.Mappings;
using POS.Api.Middleware;
using POS.Application.Abstractions.Notifications;
using POS.Application.UseCases.Payments.DTOs;
using POS.Contracts.V1.Payments;

namespace POS.Api.Notifications;

public sealed class SignalRRealtimeEventPublisher(IHubContext<PaymentHub> hub, IHttpContextAccessor accessor,
    ILogger<SignalRRealtimeEventPublisher> logger) : IRealtimeEventPublisher
{
    public async Task PublishPaymentAsync(PaymentRealtimeDto notification, CancellationToken cancellationToken)
    {
        try
        {
            var eventName = notification.Payment.Status switch
            {
                "Success" => PaymentHubEvents.PaymentSuccess,
                "Failed" => PaymentHubEvents.PaymentFailed,
                _ => null
            };
            if (eventName is null) return;

            await hub.Clients.Group(PaymentHubGroups.ForPayment(notification.Payment.PaymentId))
                .SendAsync(eventName, notification.Payment.ToResponse(), cancellationToken);

            if (accessor.HttpContext?.Items[PaymentWebhookTimingMiddleware.TimestampKey] is long receivedAt)
            {
                var elapsed = Stopwatch.GetElapsedTime(receivedAt).TotalMilliseconds;
                logger.LogInformation("Payment realtime sent for {PaymentId} in {WebhookToSignalRMilliseconds:F3} ms from webhook receipt",
                    notification.Payment.PaymentId, elapsed);
                if (elapsed >= 500)
                    logger.LogWarning("Payment realtime exceeded 500 ms for {PaymentId}: {WebhookToSignalRMilliseconds:F3} ms",
                        notification.Payment.PaymentId, elapsed);
            }
        }
        catch (Exception)
        {
            // Never log exception text: transport errors can include URLs carrying access tokens.
            logger.LogWarning("SignalR publication failed for committed payment {PaymentId}", notification.Payment.PaymentId);
        }
    }
}
