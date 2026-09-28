using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;
using POS.Application.Abstractions.Persistence;
using POS.Application.UseCases.Inventory;
using POS.Domain.Common;

namespace POS.Application.UseCases.Inventory.Stock.Queries.GetInventorySummary;

public class GetInventorySummaryQueryHandler(
    IStockEntryRepository stockEntryRepository,
    ICurrentUser currentUser) : IQueryHandler<GetInventorySummaryQuery, PagedStockEntryList>
{
    public async Task<Result<PagedStockEntryList>> Handle(
        GetInventorySummaryQuery query,
        CancellationToken cancellationToken)
    {
        if (currentUser.StoreId is null && !currentUser.IsChainOwner)
            return InventoryErrors.StoreRequired;

        var storeId = currentUser.StoreId;

        var (items, total) = await stockEntryRepository.GetPagedAsync(
            storeId,
            query.SkuId,
            query.CategoryId,
            query.Search,
            query.PageNumber,
            query.PageSize,
            cancellationToken);

        var dtos = items.Select(e => e.ToDto()).ToList();
        return new PagedStockEntryList(dtos, total, query.PageNumber, query.PageSize);
    }
}
