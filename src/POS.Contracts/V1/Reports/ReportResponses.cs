using POS.Contracts.V1.Orders;
using POS.Contracts.V1.Shifts;

namespace POS.Contracts.V1.Reports;

public record DailyRevenueChartItem(
    DateOnly Date,
    decimal Revenue,
    int OrderCount
);

public record TopSellingProductItem(
    Guid SkuId,
    string SkuCode,
    string ProductName,
    decimal QuantitySold,
    decimal TotalRevenue
);

public record DashboardAlerts(
    int LowStockItemCount,
    int ExpiringSoonBatchCount,
    int PendingStockTakeCount
);

public record DashboardSummaryResponse(
    decimal TodayRevenue,
    int TodayOrders,
    decimal AverageOrderValue,
    IReadOnlyList<DailyRevenueChartItem> RevenueChart,
    IReadOnlyList<TopSellingProductItem> TopSellingProducts,
    DashboardAlerts Alerts
);

public record RevenueByPeriodItem(
    string Period,
    decimal Revenue,
    decimal Discount,
    decimal Tax,
    decimal NetRevenue,
    int OrderCount
);

public record RevenueReportResponse(
    decimal TotalRevenue,
    decimal TotalDiscount,
    decimal TotalTax,
    decimal NetRevenue,
    int TotalOrders,
    IReadOnlyList<RevenueByPeriodItem> Periods
);

public record InventoryReportItem(
    Guid SkuId,
    string SkuCode,
    string ProductName,
    string CategoryName,
    decimal QtyOnHand,
    decimal CostPrice,
    decimal StockValue
);

public record InventoryReportResponse(
    int TotalSkus,
    decimal TotalStockValue,
    int OutOfStockCount,
    int LowStockCount,
    IReadOnlyList<InventoryReportItem> Items
);

public record SlowMovingItemResponse(
    Guid SkuId,
    string SkuCode,
    string ProductName,
    decimal QtyOnHand,
    int DaysWithoutSale,
    DateTimeOffset? LastSoldAt
);

public record ShiftReportResponse(
    ShiftSummaryResponse Shift,
    IReadOnlyList<OrderSummaryResponse> Orders
);

public record ExportReportJobResponse(
    Guid JobId,
    string Status,
    DateTimeOffset CreatedAt
);

public record ExportJobStatusResponse(
    Guid JobId,
    string Status,
    string? DownloadUrl,
    string? ErrorMessage
);
