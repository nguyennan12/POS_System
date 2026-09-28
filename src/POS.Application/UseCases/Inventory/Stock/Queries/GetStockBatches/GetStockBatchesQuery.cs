using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;
using POS.Application.UseCases.Inventory;

namespace POS.Application.UseCases.Inventory.Stock.Queries.GetStockBatches;

public record GetStockBatchesQuery(
    Guid? SkuId,
    DateOnly? ExpiryBefore
) : IQuery<List<StockBatchDto>>, IRequirePermission
{
    public string RequiredPermission => "inventory:stock:read";
}
