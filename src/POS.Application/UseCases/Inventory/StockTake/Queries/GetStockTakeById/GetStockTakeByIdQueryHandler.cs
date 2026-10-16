using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;
using POS.Application.Abstractions.Persistence;
using POS.Application.UseCases.Inventory;
using POS.Domain.Common;

namespace POS.Application.UseCases.Inventory.StockTake.Queries.GetStockTakeById;

public class GetStockTakeByIdQueryHandler(
    IStockTakeRepository stockTakeRepository,
    ICurrentUser currentUser) : IQueryHandler<GetStockTakeByIdQuery, StockTakeDetailDto>
{
    public async Task<Result<StockTakeDetailDto>> Handle(
        GetStockTakeByIdQuery query,
        CancellationToken cancellationToken)
    {
        var stockTake = await stockTakeRepository.GetByIdWithItemsAsync(query.Id, cancellationToken);
        if (stockTake is null)
            return StockTakeErrors.NotFound;

        if (!currentUser.IsChainOwner && currentUser.StoreId != stockTake.StoreId)
            return StockTakeErrors.AccessDenied;

        return stockTake.ToDetailDto();
    }
}
