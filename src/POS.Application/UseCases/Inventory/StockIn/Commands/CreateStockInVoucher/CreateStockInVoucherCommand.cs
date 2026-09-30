using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;

namespace POS.Application.UseCases.Inventory.StockIn.Commands.CreateStockInVoucher;

/// <summary>Internal input item — API layer maps from StockInVoucherItemRequest.</summary>
public record StockInItemInput(Guid SkuId, decimal Qty, decimal UnitPrice);

public record CreateStockInVoucherCommand(
    Guid SupplierId,
    IReadOnlyList<StockInItemInput> Items,
    string? Note = null
) : ICommand<StockInVoucherDetailDto>, IRequirePermission
{
    public string RequiredPermission => "inventory:create";
}

