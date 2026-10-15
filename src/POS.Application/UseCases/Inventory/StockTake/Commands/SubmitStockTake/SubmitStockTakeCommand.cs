using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;

namespace POS.Application.UseCases.Inventory.StockTake.Commands.SubmitStockTake;

public record SubmitStockTakeCommand(
    Guid StockTakeId
) : ICommand<StockTakeDetailDto>, IRequirePermission
{
    public string RequiredPermission => "stock_takes:manage";
}
