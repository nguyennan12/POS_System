using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;

namespace POS.Application.UseCases.Inventory.StockTake.Commands.UpdateStockTakeItems;

public record StockTakeItemInput(
    Guid SkuId,
    decimal ActualQty,
    string? Note = null
);

public record UpdateStockTakeItemsCommand(
    Guid StockTakeId,
    IReadOnlyList<StockTakeItemInput> Items
) : ICommand<StockTakeDetailDto>, IRequirePermission
{
    public string RequiredPermission => "stock_takes:manage";
}
