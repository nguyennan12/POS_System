using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;
using POS.Application.UseCases.Inventory;

namespace POS.Application.UseCases.Inventory.Stock.Queries.GetStockTransactions;

public record GetStockTransactionsQuery(
    Guid? SkuId,
    string? Type,
    int PageNumber,
    int PageSize
) : IQuery<PagedStockTransactionList>, IRequirePermission
{
    public string RequiredPermission => "inventory:read";
}

public record PagedStockTransactionList(
    List<StockTransactionDto> Items,
    int TotalCount,
    int PageNumber,
    int PageSize
);

