using POS.Application.UseCases.Payments.DTOs;
using POS.Domain.Orders.Enums;

namespace POS.Application.Abstractions.Payments;

public interface IWebhookSignatureVerifier
{
    PaymentMethod Method { get; }
    bool Verify(WebhookEnvelopeDto webhook);
}
