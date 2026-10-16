using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;
using POS.Contracts.V1.Payments;

namespace POS.Application.UseCases.Payments.Queries.GetPaymentStatus;

public record GetPaymentStatusQuery(Guid Id) : IQuery<PaymentStatusResponse>, IRequirePermission
{
    public string RequiredPermission => "payments:read";
}
