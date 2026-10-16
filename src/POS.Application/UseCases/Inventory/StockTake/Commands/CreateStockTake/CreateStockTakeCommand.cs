using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;

namespace POS.Application.UseCases.Inventory.StockTake.Commands.CreateStockTake;

public record CreateStockTakeCommand(
    string? Note = null
) : ICommand<StockTakeDetailDto>, IRequirePermission
{
    public string RequiredPermission => "stock_takes:manage";
}
