using POS.Application.Abstractions.Messaging;
using POS.Application.UseCases.Payments.DTOs;
using POS.Application.Abstractions.Auth;

namespace POS.Application.UseCases.Payments.Commands.ExpirePendingPayment;

public record ExpirePendingPaymentCommand(Guid Id) : ICommand<PaymentStatusDto>, IRequirePermission
{
    public string RequiredPermission => "payments:read";
}
