using POS.Application.Abstractions.Messaging;
using POS.Application.UseCases.Inventory;

namespace POS.Application.UseCases.Inventory.StockTake.Queries.GetStockTakes;

public record GetStockTakesQuery(
    Guid? StoreId = null,
    string? Status = null,
    DateTimeOffset? From = null,
    DateTimeOffset? To = null,
    int PageNumber = 1,
    int PageSize = 20
) : IQuery<PagedStockTakeList>;

public record PagedStockTakeList(
    IReadOnlyList<StockTakeSummaryDto> Items,
    int TotalCount,
    int PageNumber,
    int PageSize
);
