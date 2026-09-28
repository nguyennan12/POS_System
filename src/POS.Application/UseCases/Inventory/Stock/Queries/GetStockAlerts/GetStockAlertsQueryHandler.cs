using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;
using POS.Application.Abstractions.Persistence;
using POS.Application.UseCases.Inventory;
using POS.Domain.Common;

namespace POS.Application.UseCases.Inventory.Stock.Queries.GetStockAlerts;

public class GetStockAlertsQueryHandler(
    IStockEntryRepository stockEntryRepository,
    ICurrentUser currentUser) : IQueryHandler<GetStockAlertsQuery, List<StockAlertDto>>
{
    public async Task<Result<List<StockAlertDto>>> Handle(
        GetStockAlertsQuery query,
        CancellationToken cancellationToken)
    {
        if (currentUser.StoreId is null && !currentUser.IsChainOwner)
            return InventoryErrors.StoreRequired;

        var storeId = currentUser.StoreId ?? Guid.Empty;
        var nearExpiryDays = query.NearExpiryDays <= 0 ? 30 : query.NearExpiryDays;

        var alertEntries = await stockEntryRepository.GetAlertsAsync(storeId, nearExpiryDays, cancellationToken);

        // Lấy batches gần hết hạn để đánh dấu NearExpiry
        var expiryBefore = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(nearExpiryDays));
        var nearExpiryBatches = await stockEntryRepository.GetBatchesAsync(storeId, null, expiryBefore, cancellationToken);
        var nearExpirySkuIds = nearExpiryBatches.Select(b => b.SkuId).ToHashSet();

        var alerts = new List<StockAlertDto>();

        foreach (var entry in alertEntries)
        {
            // Cảnh báo min-stock
            if (entry.QtyOnHand <= entry.MinStock)
            {
                alerts.Add(new StockAlertDto(
                    entry.SkuId,
                    entry.Sku?.SkuCode ?? string.Empty,
                    entry.Sku?.Barcode ?? string.Empty,
                    entry.Sku?.Product?.Name ?? string.Empty,
                    entry.QtyOnHand,
                    entry.MinStock,
                    "MinStock"));
            }

            // Cảnh báo hàng cận hạn (có thể cùng SKU với min-stock)
            if (nearExpirySkuIds.Contains(entry.SkuId))
            {
                // Chỉ thêm nếu chưa có MinStock alert cho SKU này
                if (!alerts.Any(a => a.SkuId == entry.SkuId && a.AlertType == "NearExpiry"))
                {
                    alerts.Add(new StockAlertDto(
                        entry.SkuId,
                        entry.Sku?.SkuCode ?? string.Empty,
                        entry.Sku?.Barcode ?? string.Empty,
                        entry.Sku?.Product?.Name ?? string.Empty,
                        entry.QtyOnHand,
                        entry.MinStock,
                        "NearExpiry"));
                }
            }
        }

        return Result<List<StockAlertDto>>.Success(alerts);
    }
}
