using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;
using POS.Application.UseCases.Payments.DTOs;

namespace POS.Application.UseCases.Payments.Queries.AuthorizePaymentSubscription;

public record AuthorizePaymentSubscriptionQuery(Guid PaymentId) : IQuery<PaymentStatusDto>, IRequirePermission
{
    public string RequiredPermission => "payments:read";
}
