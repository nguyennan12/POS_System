using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;
using POS.Application.Abstractions.Persistence;
using POS.Application.UseCases.Inventory;
using POS.Domain.Common;

namespace POS.Application.UseCases.Inventory.Stock.Queries.GetStockBatches;

public class GetStockBatchesQueryHandler(
    IStockEntryRepository stockEntryRepository,
    ICurrentUser currentUser) : IQueryHandler<GetStockBatchesQuery, List<StockBatchDto>>
{
    public async Task<Result<List<StockBatchDto>>> Handle(
        GetStockBatchesQuery query,
        CancellationToken cancellationToken)
    {
        if (currentUser.StoreId is null && !currentUser.IsChainOwner)
            return InventoryErrors.StoreRequired;

        var storeId = currentUser.StoreId ?? Guid.Empty;

        var batches = await stockEntryRepository.GetBatchesAsync(
            storeId, query.SkuId, query.ExpiryBefore, cancellationToken);

        return Result<List<StockBatchDto>>.Success(batches.Select(b => b.ToDto()).ToList());
    }
}
