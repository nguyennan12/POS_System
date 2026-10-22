using POS.Application.UseCases.Payments.DTOs;
using POS.Domain.Common;
using POS.Domain.Orders;
using POS.Domain.Orders.Enums;

namespace POS.Application.Abstractions.Payments;

public interface IPaymentGateway
{
    PaymentMethod Method { get; }
    Task<Result<QrSessionDto>> GenerateAsync(Payment payment, QrSessionDto session, CancellationToken ct);
    Result<VerifiedWebhookDto> VerifyWebhook(WebhookEnvelopeDto webhook);
}
