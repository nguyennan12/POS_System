using System.Text.Json;
using MediatR;
using POS.Application.Abstractions.AI;
using POS.Application.Abstractions.Auth;
using POS.Application.UseCases.AI.Tools.Common;
using POS.Application.UseCases.Inventory.Stock.Queries.GetStockAlerts;

namespace POS.Application.UseCases.AI.Tools.Inventory;

public sealed record StockAlertToolItemDto(
    Guid SkuId,
    string SkuCode,
    string Barcode,
    string ProductName,
    decimal QtyOnHand,
    decimal MinStock,
    string AlertType);

public sealed class GetStockAlertsToolHandler(
    ISender sender,
    IStoreAccessPolicy storeAccessPolicy) : IAiToolHandler
{
    private static readonly HashSet<string> AllowedProperties =
        new(StringComparer.Ordinal) { "nearExpiryDays", "limit" };

    public string Name => AiToolNames.GetStockAlerts;

    public async Task<AiToolExecutionResult> ExecuteAsync(
        JsonElement arguments,
        CancellationToken cancellationToken)
    {
        if (!AiToolArgumentReader.EnsureAllowedProperties(arguments, AllowedProperties, out var error) ||
            !AiToolArgumentReader.TryReadOptionalInt(arguments, "nearExpiryDays", defaultValue: 30, min: 1, max: 365, out var nearExpiryDays, out error) ||
            !AiToolArgumentReader.TryReadOptionalInt(arguments, "limit", defaultValue: 20, min: 1, max: 50, out var limit, out error))
        {
            return error!;
        }

        var storeContext = storeAccessPolicy.EnforceStoreAccess();
        if (storeContext.IsFailure)
        {
            return AiToolErrors.FromDomainError(storeContext.Error);
        }

        var result = await sender.Send(
            new GetStockAlertsQuery(
                NearExpiryDays: nearExpiryDays,
                PageNumber: 1,
                PageSize: limit,
                StoreId: storeContext.Value),
            cancellationToken);

        if (result.IsFailure)
        {
            return AiToolErrors.FromDomainError(result.Error);
        }

        var safeAlerts = result.Value.Items
            .Take(limit)
            .Select(alert => new StockAlertToolItemDto(
                SkuId: alert.SkuId,
                SkuCode: alert.SkuCode,
                Barcode: alert.Barcode,
                ProductName: alert.ProductName,
                QtyOnHand: alert.QtyOnHand,
                MinStock: alert.MinStock,
                AlertType: alert.AlertType))
            .ToArray();

        return AiToolExecutionResult.Ok(safeAlerts, safeAlerts.Length);
    }
}
