using Microsoft.EntityFrameworkCore;
using POS.Application.Abstractions.Persistence;
using POS.Contracts.V1.Orders;
using POS.Contracts.V1.Reports;
using POS.Contracts.V1.Shifts;
using POS.Domain.Inventory.Enums;
using POS.Domain.Orders.Enums;

namespace POS.Infrastructure.Persistence.Repositories;

public class ReportRepository : IReportRepository
{
    private readonly AppDbContext _context;
    private readonly IShiftRepository _shiftRepository;

    public ReportRepository(AppDbContext context, IShiftRepository shiftRepository)
    {
        _context = context;
        _shiftRepository = shiftRepository;
    }

    public async Task<RevenueReportResponse> GetRevenueReportAsync(
        DateTimeOffset? from,
        DateTimeOffset? to,
        Guid? storeId,
        Guid? employeeId,
        CancellationToken cancellationToken = default)
    {
        var fromUtc = from?.UtcDateTime ?? DateTime.UtcNow.Date.AddDays(-30);
        var toUtc = to?.UtcDateTime ?? DateTime.UtcNow;

        var query = _context.Orders.AsNoTracking()
            .Where(o => o.PaidAt != null && o.Status != OrderStatus.Cancelled);

        query = query.Where(o => o.PaidAt >= fromUtc && o.PaidAt <= toUtc);

        if (storeId.HasValue)
        {
            query = query.Where(o => o.StoreId == storeId.Value);
        }

        if (employeeId.HasValue)
        {
            query = query.Where(o => o.CreatedBy == employeeId.Value);
        }

        var orderRecords = await query
            .Select(o => new
            {
                PaidAt = o.PaidAt!.Value,
                o.Subtotal,
                o.DiscountTotal,
                o.TaxTotal,
                o.GrandTotal
            })
            .ToListAsync(cancellationToken);

        var periods = orderRecords
            .GroupBy(o => o.PaidAt.ToString("yyyy-MM-dd"))
            .OrderBy(g => g.Key)
            .Select(g => new RevenueByPeriodItem(
                Period: g.Key,
                Revenue: g.Sum(x => x.Subtotal),
                Discount: g.Sum(x => x.DiscountTotal),
                Tax: g.Sum(x => x.TaxTotal),
                NetRevenue: g.Sum(x => x.GrandTotal),
                OrderCount: g.Count()
            ))
            .ToList();

        var totalRevenue = orderRecords.Sum(x => x.Subtotal);
        var totalDiscount = orderRecords.Sum(x => x.DiscountTotal);
        var totalTax = orderRecords.Sum(x => x.TaxTotal);
        var netRevenue = orderRecords.Sum(x => x.GrandTotal);
        var totalOrders = orderRecords.Count;

        return new RevenueReportResponse(
            TotalRevenue: totalRevenue,
            TotalDiscount: totalDiscount,
            TotalTax: totalTax,
            NetRevenue: netRevenue,
            TotalOrders: totalOrders,
            Periods: periods);
    }

    public async Task<IReadOnlyList<TopSellingProductItem>> GetTopSellingAsync(
        DateTimeOffset? from,
        DateTimeOffset? to,
        int limit,
        Guid? storeId = null,
        CancellationToken cancellationToken = default)
    {
        var fromUtc = from?.UtcDateTime ?? DateTime.UtcNow.Date.AddDays(-30);
        var toUtc = to?.UtcDateTime ?? DateTime.UtcNow;
        var takeCount = limit > 0 ? limit : 5;

        var query = _context.OrderItems.AsNoTracking()
            .Where(oi => oi.Order.PaidAt != null && oi.Order.Status != OrderStatus.Cancelled);

        query = query.Where(oi => oi.Order.PaidAt >= fromUtc && oi.Order.PaidAt <= toUtc);

        if (storeId.HasValue)
        {
            query = query.Where(oi => oi.Order.StoreId == storeId.Value);
        }

        var grouped = await query
            .GroupBy(oi => oi.SkuId)
            .Select(g => new
            {
                SkuId = g.Key,
                QuantitySold = g.Sum(x => x.Qty),
                TotalRevenue = g.Sum(x => x.LineTotal)
            })
            .OrderByDescending(x => x.QuantitySold)
            .Take(takeCount)
            .ToListAsync(cancellationToken);

        if (!grouped.Any())
        {
            return Array.Empty<TopSellingProductItem>();
        }

        var skuIds = grouped.Select(x => x.SkuId).ToList();
        var skuDict = await _context.Skus.AsNoTracking()
            .Include(s => s.Product)
            .Where(s => skuIds.Contains(s.Id))
            .ToDictionaryAsync(s => s.Id, cancellationToken);

        return grouped
            .Select(g =>
            {
                skuDict.TryGetValue(g.SkuId, out var sku);
                return new TopSellingProductItem(
                    SkuId: g.SkuId,
                    SkuCode: sku?.SkuCode ?? string.Empty,
                    ProductName: sku?.Product?.Name ?? string.Empty,
                    QuantitySold: g.QuantitySold,
                    TotalRevenue: g.TotalRevenue);
            })
            .ToList();
    }

    public async Task<InventoryReportResponse> GetInventoryReportAsync(
        Guid? storeId,
        Guid? categoryId,
        CancellationToken cancellationToken = default)
    {
        var query = _context.StockEntries.AsNoTracking()
            .Include(se => se.Sku)
                .ThenInclude(s => s.Product)
                    .ThenInclude(p => p.Category)
            .AsQueryable();

        if (storeId.HasValue)
        {
            query = query.Where(se => se.StoreId == storeId.Value);
        }

        if (categoryId.HasValue)
        {
            query = query.Where(se => se.Sku.Product.CategoryId == categoryId.Value);
        }

        var entries = await query.ToListAsync(cancellationToken);

        var items = entries
            .Select(e =>
            {
                var costPrice = e.AverageCost > 0 ? e.AverageCost : (e.Sku?.CostPrice ?? 0);
                var stockValue = e.QtyOnHand * costPrice;

                return new InventoryReportItem(
                    SkuId: e.SkuId,
                    SkuCode: e.Sku?.SkuCode ?? string.Empty,
                    ProductName: e.Sku?.Product?.Name ?? string.Empty,
                    CategoryName: e.Sku?.Product?.Category?.Name ?? string.Empty,
                    QtyOnHand: e.QtyOnHand,
                    CostPrice: costPrice,
                    StockValue: stockValue);
            })
            .ToList();

        var totalSkus = items.Count;
        var totalStockValue = items.Sum(i => i.StockValue);
        var outOfStockCount = entries.Count(e => e.QtyOnHand <= 0);
        var lowStockCount = entries.Count(e => e.QtyOnHand > 0 && e.QtyOnHand <= e.MinStock);

        return new InventoryReportResponse(
            TotalSkus: totalSkus,
            TotalStockValue: totalStockValue,
            OutOfStockCount: outOfStockCount,
            LowStockCount: lowStockCount,
            Items: items);
    }

    public async Task<IReadOnlyList<SlowMovingItemResponse>> GetSlowMovingItemsAsync(
        int days,
        Guid? storeId = null,
        CancellationToken cancellationToken = default)
    {
        var daysThreshold = days > 0 ? days : 30;
        var nowUtc = DateTime.UtcNow;

        var stockQuery = _context.StockEntries.AsNoTracking()
            .Include(se => se.Sku)
                .ThenInclude(s => s.Product)
            .Where(se => se.QtyOnHand > 0);

        if (storeId.HasValue)
        {
            stockQuery = stockQuery.Where(se => se.StoreId == storeId.Value);
        }

        var stockEntries = await stockQuery.ToListAsync(cancellationToken);
        if (!stockEntries.Any())
        {
            return Array.Empty<SlowMovingItemResponse>();
        }

        var skuIds = stockEntries.Select(e => e.SkuId).Distinct().ToList();

        var lastSales = await _context.OrderItems.AsNoTracking()
            .Where(oi => skuIds.Contains(oi.SkuId) && oi.Order.PaidAt != null && oi.Order.Status != OrderStatus.Cancelled)
            .GroupBy(oi => oi.SkuId)
            .Select(g => new
            {
                SkuId = g.Key,
                LastSoldAt = g.Max(oi => oi.Order.PaidAt)
            })
            .ToDictionaryAsync(x => x.SkuId, x => x.LastSoldAt, cancellationToken);

        var slowMovingItems = new List<SlowMovingItemResponse>();

        foreach (var entry in stockEntries)
        {
            lastSales.TryGetValue(entry.SkuId, out var lastSoldAt);

            int daysWithoutSale;
            if (lastSoldAt.HasValue)
            {
                daysWithoutSale = Math.Max(0, (int)(nowUtc - lastSoldAt.Value).TotalDays);
            }
            else
            {
                // Never sold: calculate days since SKU creation
                var daysSinceCreation = Math.Max(0, (int)(nowUtc - (entry.Sku?.CreatedAt ?? nowUtc)).TotalDays);
                daysWithoutSale = Math.Max(daysThreshold, daysSinceCreation);
            }

            if (daysWithoutSale >= daysThreshold)
            {
                slowMovingItems.Add(new SlowMovingItemResponse(
                    SkuId: entry.SkuId,
                    SkuCode: entry.Sku?.SkuCode ?? string.Empty,
                    ProductName: entry.Sku?.Product?.Name ?? string.Empty,
                    QtyOnHand: entry.QtyOnHand,
                    DaysWithoutSale: daysWithoutSale,
                    LastSoldAt: lastSoldAt.HasValue ? new DateTimeOffset(lastSoldAt.Value, TimeSpan.Zero) : null
                ));
            }
        }

        return slowMovingItems
            .OrderByDescending(x => x.DaysWithoutSale)
            .ToList();
    }

    public async Task<ShiftReportResponse?> GetShiftReportAsync(
        Guid shiftId,
        CancellationToken cancellationToken = default)
    {
        var shift = await _context.Shifts.AsNoTracking()
            .Include(s => s.Employee)
            .Include(s => s.Register)
            .FirstOrDefaultAsync(s => s.Id == shiftId, cancellationToken);

        if (shift is null)
        {
            return null;
        }

        var sales = await _shiftRepository.GetShiftSalesAsync(shiftId, cancellationToken);
        var expectedCash = shift.OpeningCash + sales.CashSales - sales.Refunds;
        decimal? difference = shift.ActualCash.HasValue ? shift.ActualCash.Value - expectedCash : null;

        var shiftSummary = new ShiftSummaryResponse(
            ShiftId: shift.Id,
            RegisterId: shift.RegisterId,
            RegisterName: shift.Register?.Name ?? "Quáº§y",
            EmployeeId: shift.EmployeeId,
            EmployeeName: shift.Employee?.Name ?? string.Empty,
            OpeningCash: shift.OpeningCash,
            TotalCashSales: sales.CashSales,
            TotalCardSales: sales.CardSales,
            TotalQrSales: sales.QrSales,
            TotalRefunds: sales.Refunds,
            ExpectedCash: expectedCash,
            ActualCash: shift.ActualCash,
            Difference: difference,
            OrderCount: sales.OrderCount,
            OpenedAt: new DateTimeOffset(shift.OpenedAt, TimeSpan.Zero),
            ClosedAt: shift.ClosedAt.HasValue ? new DateTimeOffset(shift.ClosedAt.Value, TimeSpan.Zero) : null);

        var orders = await _context.Orders.AsNoTracking()
            .Include(o => o.Customer)
            .Include(o => o.Items)
            .Where(o => o.ShiftId == shiftId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync(cancellationToken);

        var orderSummaries = orders
            .Select(o => new OrderSummaryResponse(
                Id: o.Id,
                StoreId: o.StoreId,
                ShiftId: o.ShiftId,
                CustomerId: o.CustomerId,
                CustomerName: o.Customer?.Name,
                CustomerPhone: o.Customer?.Phone,
                Status: o.Status.ToString(),
                CurrencyCode: o.CurrencyCode,
                GrandTotal: o.GrandTotal,
                TotalItems: o.Items.Count,
                CreatedAt: new DateTimeOffset(o.CreatedAt, TimeSpan.Zero),
                PaidAt: o.PaidAt.HasValue ? new DateTimeOffset(o.PaidAt.Value, TimeSpan.Zero) : null))
            .ToList();

        return new ShiftReportResponse(shiftSummary, orderSummaries);
    }

    public async Task<DashboardSummaryResponse> GetDashboardSummaryAsync(
        Guid? storeId,
        int days = 7,
        CancellationToken cancellationToken = default)
    {
        var chartDays = Math.Clamp(days <= 0 ? 7 : days, 7, 30);
        var nowUtc = DateTime.UtcNow;
        var todayStartUtc = nowUtc.Date;
        var tomorrowStartUtc = todayStartUtc.AddDays(1);
        var yesterdayStartUtc = todayStartUtc.AddDays(-1);
        var chartStartUtc = todayStartUtc.AddDays(-(chartDays - 1));

        var ordersQuery = _context.Orders.AsNoTracking()
            .Where(o => o.PaidAt != null && o.Status != OrderStatus.Cancelled);

        if (storeId.HasValue)
        {
            ordersQuery = ordersQuery.Where(o => o.StoreId == storeId.Value);
        }

        var fetchStartUtc = chartStartUtc < yesterdayStartUtc ? chartStartUtc : yesterdayStartUtc;

        var recentOrders = await ordersQuery
            .Where(o => o.PaidAt >= fetchStartUtc && o.PaidAt < tomorrowStartUtc)
            .Select(o => new
            {
                PaidAt = o.PaidAt!.Value,
                Date = DateOnly.FromDateTime(o.PaidAt!.Value),
                o.GrandTotal
            })
            .ToListAsync(cancellationToken);

        // 1. Today metrics
        var todayOrders = recentOrders.Where(o => o.PaidAt >= todayStartUtc && o.PaidAt < tomorrowStartUtc).ToList();
        var todayRevenue = todayOrders.Sum(o => o.GrandTotal);
        var todayOrderCount = todayOrders.Count;
        var avgOrderValue = todayOrderCount > 0 ? Math.Round(todayRevenue / todayOrderCount, 2) : 0m;

        // 2. Yesterday revenue & growth percentage
        var yesterdayOrders = recentOrders.Where(o => o.PaidAt >= yesterdayStartUtc && o.PaidAt < todayStartUtc).ToList();
        var yesterdayRevenue = yesterdayOrders.Sum(o => o.GrandTotal);

        decimal growthPercentage;
        if (yesterdayRevenue == 0m)
        {
            growthPercentage = todayRevenue > 0m ? 100m : 0m;
        }
        else
        {
            growthPercentage = Math.Round(((todayRevenue - yesterdayRevenue) / yesterdayRevenue) * 100m, 2);
        }

        // 3. Dynamic revenue chart (7-30 days)
        var chartItems = new List<DailyRevenueChartItem>(chartDays);
        for (int i = chartDays - 1; i >= 0; i--)
        {
            var date = DateOnly.FromDateTime(todayStartUtc.AddDays(-i));
            var dayOrders = recentOrders.Where(x => x.Date == date).ToList();
            chartItems.Add(new DailyRevenueChartItem(
                Date: date,
                Revenue: dayOrders.Sum(x => x.GrandTotal),
                OrderCount: dayOrders.Count));
        }

        // 4. Top 5 selling products (last 30 days)
        var topSelling = await GetTopSellingAsync(
            from: nowUtc.AddDays(-30),
            to: nowUtc,
            limit: 5,
            storeId: storeId,
            cancellationToken: cancellationToken);

        // 5. Dashboard alerts
        var stockEntriesQuery = _context.StockEntries.AsNoTracking().AsQueryable();
        var batchesQuery = _context.StockBatches.AsNoTracking().Where(b => b.Qty > 0 && b.ExpiryDate != null);
        var stockTakesQuery = _context.StockTakes.AsNoTracking().AsQueryable();

        if (storeId.HasValue)
        {
            stockEntriesQuery = stockEntriesQuery.Where(se => se.StoreId == storeId.Value);
            batchesQuery = batchesQuery.Where(b => b.StoreId == storeId.Value);
            stockTakesQuery = stockTakesQuery.Where(st => st.StoreId == storeId.Value);
        }

        var lowStockCount = await stockEntriesQuery.CountAsync(se => se.QtyOnHand <= se.MinStock, cancellationToken);

        var thirtyDaysFromNow = DateOnly.FromDateTime(todayStartUtc.AddDays(30));
        var todayDateOnly = DateOnly.FromDateTime(todayStartUtc);
        var expiringSoonCount = await batchesQuery.CountAsync(b => b.ExpiryDate <= thirtyDaysFromNow && b.ExpiryDate >= todayDateOnly, cancellationToken);

        var pendingStockTakeCount = await stockTakesQuery.CountAsync(st => st.Status == StockTakeStatus.Pending, cancellationToken);

        var alerts = new DashboardAlerts(
            LowStockItemCount: lowStockCount,
            ExpiringSoonBatchCount: expiringSoonCount,
            PendingStockTakeCount: pendingStockTakeCount);

        return new DashboardSummaryResponse(
            TodayRevenue: todayRevenue,
            TodayOrders: todayOrderCount,
            AverageOrderValue: avgOrderValue,
            RevenueGrowthPercentage: growthPercentage,
            RevenueChart: chartItems,
            TopSellingProducts: topSelling,
            Alerts: alerts);
    }
}
