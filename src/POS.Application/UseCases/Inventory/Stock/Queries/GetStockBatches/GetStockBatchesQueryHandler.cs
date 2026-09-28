using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;
using POS.Application.Abstractions.Persistence;
using POS.Application.UseCases.Inventory;
using POS.Domain.Common;

namespace POS.Application.UseCases.Inventory.Stock.Queries.GetStockBatches;

public class GetStockBatchesQueryHandler(
    IStockEntryRepository stockEntryRepository,
    ICurrentUser currentUser) : IQueryHandler<GetStockBatchesQuery, PagedStockBatchList>
{
    public async Task<Result<PagedStockBatchList>> Handle(
        GetStockBatchesQuery query,
        CancellationToken cancellationToken)
    {
        if (currentUser.StoreId is null && !currentUser.IsChainOwner)
            return InventoryErrors.StoreRequired;

        var storeId = currentUser.StoreId ?? Guid.Empty;

        var (items, total) = await stockEntryRepository.GetBatchesAsync(
            storeId, query.SkuId, query.ExpiryBefore, query.PageNumber, query.PageSize, cancellationToken);

        var dtos = items.Select(b => b.ToDto()).ToList();
        return Result<PagedStockBatchList>.Success(
            new PagedStockBatchList(dtos, total, query.PageNumber, query.PageSize));
    }
}
