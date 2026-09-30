using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;
using POS.Application.UseCases.Inventory;

namespace POS.Application.UseCases.Inventory.Stock.Queries.GetStockAlerts;

/// <summary>
/// Láº¥y danh sÃ¡ch cáº£nh bÃ¡o tá»“n kho:
/// - AlertType = "MinStock": qty_on_hand &lt;= min_stock
/// - AlertType = "NearExpiry": cÃ³ lÃ´ hÃ ng háº¿t háº¡n trong <see cref="NearExpiryDays"/> ngÃ y tá»›i
/// </summary>
public record GetStockAlertsQuery(
    int NearExpiryDays = 30,
    int PageNumber = 1,
    int PageSize = 20
) : IQuery<PagedStockAlertList>, IRequirePermission
{
    public string RequiredPermission => "inventory:read";
}

public record PagedStockAlertList(
    IReadOnlyList<StockAlertDto> Items,
    int TotalCount,
    int PageNumber,
    int PageSize
);

