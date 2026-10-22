using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;
using POS.Application.UseCases.Payments.DTOs;

namespace POS.Application.UseCases.Payments.Commands.GenerateQrPayment;

public record GenerateQrPaymentCommand(Guid OrderId, string Method) : ICommand<QrPaymentDto>, IRequirePermission
{
    public string RequiredPermission => "payments:create_own";
}
