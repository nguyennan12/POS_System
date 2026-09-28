using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;
using POS.Application.UseCases.Inventory;

namespace POS.Application.UseCases.Inventory.Stock.Queries.GetInventorySummary;

public record GetInventorySummaryQuery(
    Guid? SkuId,
    Guid? CategoryId,
    string? Search,
    int PageNumber,
    int PageSize
) : IQuery<PagedStockEntryList>, IRequirePermission
{
    public string RequiredPermission => "inventory:stock:read";
}

public record PagedStockEntryList(
    List<StockEntryDto> Items,
    int TotalCount,
    int PageNumber,
    int PageSize
);
