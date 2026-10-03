using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;

namespace POS.Application.UseCases.Promotions.Commands.DeleteVoucher;

public record DeleteVoucherCommand(Guid Id) : ICommand<bool>, IRequirePermission
{
    public string RequiredPermission => "discounts:delete";
}
