using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;
using POS.Application.UseCases.Inventory;

namespace POS.Application.UseCases.Inventory.StockIn.Commands.CancelStockInVoucher;

public record CancelStockInVoucherCommand(Guid VoucherId)
    : ICommand<StockInVoucherDetailDto>, IRequirePermission
{
    public string RequiredPermission => "stock_in_vouchers:manage";
}

