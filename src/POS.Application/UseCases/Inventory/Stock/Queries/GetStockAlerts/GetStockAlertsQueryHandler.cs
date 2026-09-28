using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;
using POS.Application.Abstractions.Persistence;
using POS.Application.UseCases.Inventory;
using POS.Domain.Common;

namespace POS.Application.UseCases.Inventory.Stock.Queries.GetStockAlerts;

public class GetStockAlertsQueryHandler(
    IStockEntryRepository stockEntryRepository,
    ICurrentUser currentUser) : IQueryHandler<GetStockAlertsQuery, PagedStockAlertList>
{
    public async Task<Result<PagedStockAlertList>> Handle(
        GetStockAlertsQuery query,
        CancellationToken cancellationToken)
    {
        if (currentUser.StoreId is null && !currentUser.IsChainOwner)
            return InventoryErrors.StoreRequired;

        var storeId = currentUser.StoreId;
        var nearExpiryDays = query.NearExpiryDays <= 0 ? 30 : query.NearExpiryDays;

        var (alertEntries, total) = await stockEntryRepository.GetAlertsAsync(
            storeId, nearExpiryDays, query.PageNumber, query.PageSize, cancellationToken);

        // Lấy batches gần hết hạn để đánh dấu NearExpiry
        var expiryBefore = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(nearExpiryDays));
        // We only care if any batch exists for this SKU that is near expiry, so paging is not needed here
        // We can just check the db directly, or since GetBatches is now paged, just take 100 max
        var (nearExpiryBatches, _) = await stockEntryRepository.GetBatchesAsync(
            storeId, null, expiryBefore, 1, 1000, cancellationToken);
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
                // SKU entry is processed only once, no need to check duplicates in alerts list
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

        return Result<PagedStockAlertList>.Success(
            new PagedStockAlertList(alerts, total, query.PageNumber, query.PageSize));
    }
}
