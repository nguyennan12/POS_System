using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;
using POS.Application.UseCases.Inventory;

namespace POS.Application.UseCases.Inventory.StockIn.Queries.GetStockInVoucherById;

public record GetStockInVoucherByIdQuery(Guid VoucherId)
    : IQuery<StockInVoucherDetailDto>, IRequirePermission
{
    public string RequiredPermission => "stock_in_vouchers:read";
}

