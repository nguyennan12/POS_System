using POS.Application.UseCases.Payments.DTOs;

namespace POS.Application.Abstractions.Notifications;

public interface IRealtimeEventPublisher
{
    Task PublishPaymentAsync(PaymentRealtimeDto notification, CancellationToken cancellationToken);
}
