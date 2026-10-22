using POS.Application.Abstractions.Notifications;
using POS.Application.UseCases.Payments.DTOs;

namespace POS.Infrastructure.Notifications;

// TODO T47: replace this registration with the SignalR publisher.
public sealed class NoOpRealtimeEventPublisher : IRealtimeEventPublisher
{
    public Task PublishPaymentAsync(PaymentRealtimeDto notification, CancellationToken cancellationToken) => Task.CompletedTask;
}
