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

        var storeId = currentUser.IsChainOwner ? null : currentUser.StoreId;

        // nearExpiryDays = 0 hợp lệ: lọc các lô hết hạn ngay hôm nay
        var nearExpiryDays = Math.Max(0, query.NearExpiryDays);

        var (alertEntries, total, nearExpirySkuIds) = await stockEntryRepository.GetAlertsAsync(
            storeId, nearExpiryDays, query.PageNumber, query.PageSize, cancellationToken);

        var alerts = new List<StockAlertDto>();

        foreach (var entry in alertEntries)
        {
            var skuCode   = entry.Sku?.SkuCode ?? string.Empty;
            var barcode   = entry.Sku?.Barcode ?? string.Empty;
            var name      = entry.Sku?.Product?.Name ?? string.Empty;

            // Cảnh báo tồn kho dưới mức tối thiểu
            if (entry.QtyOnHand <= entry.MinStock)
            {
                alerts.Add(new StockAlertDto(
                    entry.SkuId, skuCode, barcode, name,
                    entry.QtyOnHand, entry.MinStock, "MinStock"));
            }

            // Cảnh báo lô cận hạn (có thể cùng SKU với MinStock → thêm cả 2)
            if (nearExpirySkuIds.Contains(entry.SkuId))
            {
                alerts.Add(new StockAlertDto(
                    entry.SkuId, skuCode, barcode, name,
                    entry.QtyOnHand, entry.MinStock, "NearExpiry"));
            }
        }

        // totalCount từ repository phản ánh số StockEntry unique thoả điều kiện
        return Result<PagedStockAlertList>.Success(
            new PagedStockAlertList(alerts, total, query.PageNumber, query.PageSize));
    }
}
