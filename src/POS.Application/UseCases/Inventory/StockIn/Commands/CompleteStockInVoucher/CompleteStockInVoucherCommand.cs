using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;
using POS.Application.UseCases.Inventory;

namespace POS.Application.UseCases.Inventory.StockIn.Commands.CompleteStockInVoucher;

public record CompleteStockInVoucherCommand(Guid VoucherId)
    : ICommand<StockInVoucherDetailDto>, IRequirePermission
{
    public string RequiredPermission => "inventory:update";
}
