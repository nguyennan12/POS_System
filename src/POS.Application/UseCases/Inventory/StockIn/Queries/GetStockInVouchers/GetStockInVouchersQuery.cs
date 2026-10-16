using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;
using POS.Application.UseCases.Inventory;

namespace POS.Application.UseCases.Inventory.StockIn.Queries.GetStockInVouchers;

public record GetStockInVouchersQuery(
    Guid? SupplierId,
    string? Status,
    DateTimeOffset? From,
    DateTimeOffset? To,
    int PageNumber,
    int PageSize
) : IQuery<PagedStockInVoucherList>, IRequirePermission
{
    public string RequiredPermission => "stock_in_vouchers:read";
}

public record PagedStockInVoucherList(
    List<StockInVoucherSummaryDto> Items,
    int TotalCount,
    int PageNumber,
    int PageSize
);
