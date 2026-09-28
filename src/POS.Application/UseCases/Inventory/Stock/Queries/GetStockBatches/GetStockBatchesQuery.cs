using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;
using POS.Application.UseCases.Inventory;

namespace POS.Application.UseCases.Inventory.Stock.Queries.GetStockBatches;

public record GetStockBatchesQuery(
    Guid? SkuId,
    DateOnly? ExpiryBefore,
    int PageNumber = 1,
    int PageSize = 20
) : IQuery<PagedStockBatchList>, IRequirePermission
{
    public string RequiredPermission => "inventory:stock:read";
}

public record PagedStockBatchList(
    IReadOnlyList<StockBatchDto> Items,
    int TotalCount,
    int PageNumber,
    int PageSize
);
