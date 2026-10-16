using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;
using POS.Application.UseCases.Inventory;

namespace POS.Application.UseCases.Inventory.Stock.Commands.DisposeStock;

public record DisposeStockCommand(
    Guid SkuId,
    decimal Qty,
    string Note,
    Guid? BatchId = null
) : ICommand<StockTransactionDto>, IRequirePermission
{
    public string RequiredPermission => "inventory:dispose";
}

