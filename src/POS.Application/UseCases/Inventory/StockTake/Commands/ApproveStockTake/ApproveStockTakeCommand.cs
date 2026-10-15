using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;

namespace POS.Application.UseCases.Inventory.StockTake.Commands.ApproveStockTake;

public record ApproveStockTakeCommand(
    Guid StockTakeId
) : ICommand<StockTakeDetailDto>, IRequirePermission
{
    public string RequiredPermission => "stock_takes:approve";
}
