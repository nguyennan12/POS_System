using POS.Contracts.V1.Reports;

namespace POS.Application.Abstractions.Persistence;

public interface IReportRepository
{
    Task<RevenueReportResponse> GetRevenueReportAsync(
        DateTimeOffset? from,
        DateTimeOffset? to,
        Guid? storeId,
        Guid? employeeId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TopSellingProductItem>> GetTopSellingAsync(
        DateTimeOffset? from,
        DateTimeOffset? to,
        int limit,
        Guid? storeId = null,
        CancellationToken cancellationToken = default);

    Task<InventoryReportResponse> GetInventoryReportAsync(
        Guid? storeId,
        Guid? categoryId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SlowMovingItemResponse>> GetSlowMovingItemsAsync(
        int days,
        Guid? storeId = null,
        CancellationToken cancellationToken = default);

    Task<ShiftReportResponse?> GetShiftReportAsync(
        Guid shiftId,
        CancellationToken cancellationToken = default);

    Task<DashboardSummaryResponse> GetDashboardSummaryAsync(
        Guid? storeId,
        int days = 7,
        CancellationToken cancellationToken = default);
}
