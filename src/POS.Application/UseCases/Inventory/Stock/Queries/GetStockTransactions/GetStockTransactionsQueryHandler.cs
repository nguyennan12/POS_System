using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;
using POS.Application.Abstractions.Persistence;
using POS.Application.UseCases.Inventory;
using POS.Domain.Common;

namespace POS.Application.UseCases.Inventory.Stock.Queries.GetStockTransactions;

public class GetStockTransactionsQueryHandler(
    IStockTransactionRepository stockTransactionRepository,
    ICurrentUser currentUser) : IQueryHandler<GetStockTransactionsQuery, PagedStockTransactionList>
{
    public async Task<Result<PagedStockTransactionList>> Handle(
        GetStockTransactionsQuery query,
        CancellationToken cancellationToken)
    {
        if (currentUser.StoreId is null && !currentUser.IsChainOwner)
            return InventoryErrors.StoreRequired;

        var storeId = currentUser.StoreId ?? Guid.Empty;

        var (items, total) = await stockTransactionRepository.GetPagedAsync(
            storeId,
            query.SkuId,
            query.Type,
            query.PageNumber,
            query.PageSize,
            cancellationToken);

        var dtos = items.Select(t => t.ToDto()).ToList();
        return new PagedStockTransactionList(dtos, total, query.PageNumber, query.PageSize);
    }
}
