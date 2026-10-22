using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;
using POS.Application.UseCases.Payments.DTOs;

namespace POS.Application.UseCases.Payments.Queries.GetPaymentStatus;

public record GetPaymentStatusQuery(Guid Id) : IQuery<PaymentStatusDto>, IRequirePermission
{
    public string RequiredPermission => "payments:read";
}
