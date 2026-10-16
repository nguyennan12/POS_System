using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;
using POS.Application.Abstractions.Persistence;
using POS.Application.UseCases.Inventory;
using POS.Domain.Common;

namespace POS.Application.UseCases.Inventory.StockTake.Queries.GetStockTakes;

public class GetStockTakesQueryHandler(
    IStockTakeRepository stockTakeRepository,
    ICurrentUser currentUser) : IQueryHandler<GetStockTakesQuery, PagedStockTakeList>
{
    public async Task<Result<PagedStockTakeList>> Handle(
        GetStockTakesQuery query,
        CancellationToken cancellationToken)
    {
        if (currentUser.StoreId is null && !currentUser.IsChainOwner)
            return StockTakeErrors.StoreRequired;

        var storeId = currentUser.IsChainOwner ? query.StoreId : currentUser.StoreId;

        var (items, totalCount) = await stockTakeRepository.GetPagedAsync(
            storeId,
            query.Status,
            query.From,
            query.To,
            query.PageNumber,
            query.PageSize,
            cancellationToken);

        var dtos = items.Select(x => x.ToSummaryDto()).ToList();

        return new PagedStockTakeList(dtos, totalCount, query.PageNumber, query.PageSize);
    }
}
