using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;
using POS.Application.Abstractions.Persistence;
using POS.Application.UseCases.Inventory;
using POS.Domain.Common;

namespace POS.Application.UseCases.Inventory.StockIn.Queries.GetStockInVouchers;

public class GetStockInVouchersQueryHandler(
    IStockInVoucherRepository stockInVoucherRepository,
    ICurrentUser currentUser) : IQueryHandler<GetStockInVouchersQuery, PagedStockInVoucherList>
{
    public async Task<Result<PagedStockInVoucherList>> Handle(
        GetStockInVouchersQuery query,
        CancellationToken cancellationToken)
    {
        if (currentUser.StoreId is null && !currentUser.IsChainOwner)
            return StockInErrors.StoreRequired;

        var storeId = currentUser.StoreId ?? Guid.Empty;

        var (items, total) = await stockInVoucherRepository.GetPagedAsync(
            storeId,
            query.SupplierId,
            query.Status,
            query.From,
            query.To,
            query.PageNumber,
            query.PageSize,
            cancellationToken);

        var dtos = items.Select(v => v.ToSummaryDto()).ToList();
        return new PagedStockInVoucherList(dtos, total, query.PageNumber, query.PageSize);
    }
}
