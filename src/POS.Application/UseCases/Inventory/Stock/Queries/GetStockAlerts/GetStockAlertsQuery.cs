using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;
using POS.Application.UseCases.Inventory;

namespace POS.Application.UseCases.Inventory.Stock.Queries.GetStockAlerts;

/// <summary>
/// Lấy danh sách cảnh báo tồn kho:
/// - AlertType = "MinStock": qty_on_hand &lt;= min_stock
/// - AlertType = "NearExpiry": có lô hàng hết hạn trong <see cref="NearExpiryDays"/> ngày tới
/// </summary>
public record GetStockAlertsQuery(
    int NearExpiryDays = 30,
    int PageNumber = 1,
    int PageSize = 20
) : IQuery<PagedStockAlertList>, IRequirePermission
{
    public string RequiredPermission => "inventory:stock:read";
}

public record PagedStockAlertList(
    IReadOnlyList<StockAlertDto> Items,
    int TotalCount,
    int PageNumber,
    int PageSize
);
