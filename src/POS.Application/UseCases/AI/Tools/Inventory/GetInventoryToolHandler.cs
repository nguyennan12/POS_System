using System.Text.Json;
using MediatR;
using POS.Application.Abstractions.AI;
using POS.Application.Abstractions.Auth;
using POS.Application.UseCases.AI.Tools.Common;
using POS.Application.UseCases.Inventory.Stock.Queries.GetInventorySummary;

namespace POS.Application.UseCases.AI.Tools.Inventory;

public sealed record InventoryToolDto(
    Guid SkuId,
    string SkuCode,
    string Barcode,
    string ProductName,
    decimal QtyOnHand,
    decimal MinStock,
    DateTimeOffset LastUpdated);

public sealed class GetInventoryToolHandler(
    ISender sender,
    IStoreAccessPolicy storeAccessPolicy) : IAiToolHandler
{
    private static readonly HashSet<string> AllowedProperties =
        new(StringComparer.Ordinal) { "skuId" };

    public string Name => AiToolNames.GetInventory;

    public async Task<AiToolExecutionResult> ExecuteAsync(
        JsonElement arguments,
        CancellationToken cancellationToken)
    {
        if (!AiToolArgumentReader.EnsureAllowedProperties(arguments, AllowedProperties, out var error) ||
            !AiToolArgumentReader.TryReadRequiredGuid(arguments, "skuId", out var skuId, out error))
        {
            return error!;
        }

        var storeContext = storeAccessPolicy.EnforceStoreAccess();
        if (storeContext.IsFailure)
        {
            return AiToolErrors.FromDomainError(storeContext.Error);
        }

        var result = await sender.Send(
            new GetInventorySummaryQuery(
                SkuId: skuId,
                CategoryId: null,
                Search: null,
                PageNumber: 1,
                PageSize: 20,
                StoreId: storeContext.Value),
            cancellationToken);

        if (result.IsFailure)
        {
            return AiToolErrors.FromDomainError(result.Error);
        }

        var entry = result.Value.Items.FirstOrDefault(i => i.SkuId == skuId);
        if (entry is null)
        {
            return AiToolErrors.ResourceNotFound("Không tìm thấy dữ liệu tồn kho cho SKU này tại chi nhánh hiện tại.");
        }

        var safeInventory = new InventoryToolDto(
            SkuId: entry.SkuId,
            SkuCode: entry.SkuCode,
            Barcode: entry.Barcode,
            ProductName: entry.ProductName,
            QtyOnHand: entry.QtyOnHand,
            MinStock: entry.MinStock,
            LastUpdated: entry.LastUpdated);

        return AiToolExecutionResult.Ok(safeInventory, 1);
    }
}
