using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;
using POS.Application.UseCases.Orders.DTOs;

namespace POS.Application.UseCases.Orders.Commands.ApplyVoucher;

public record ApplyVoucherCommand(
    Guid OrderId,
    string Code
) : ICommand<OrderDetailDto>, IRequirePermission
{
    public string RequiredPermission => "vouchers:apply_own";
}
