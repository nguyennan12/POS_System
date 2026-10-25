using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using POS.Contracts.V1.Common;
using POS.Contracts.V1.Reports;
using POS.WinUI.Core.Constants;
using POS.WinUI.Core.Services;

namespace POS.WinUI.Core.ApiClients;

/// <summary>
/// Client gọi REST API cho phân hệ Báo cáo & Dashboard kinh doanh (Reports & Analytics)
/// </summary>
public sealed class ReportApiClient : BaseApiClient
{
    public ReportApiClient(HttpClient http, SessionService session)
        : base(http, session) { }

    /// <summary>
    /// Lấy chỉ số Dashboard kinh doanh thời gian thực
    /// </summary>
    public Task<ApiResponse<DashboardSummaryResponse>?> GetDashboardSummaryAsync(
        Guid? storeId = null,
        int days = 7,
        CancellationToken ct = default)
    {
        var queryParams = new List<string> { $"days={days}" };
        if (storeId.HasValue)
        {
            queryParams.Add($"storeId={storeId.Value}");
        }

        var url = $"{ApiRoutes.Reports.Dashboard}?{string.Join("&", queryParams)}";
        return GetAsync<ApiResponse<DashboardSummaryResponse>>(url, ct);
    }

    /// <summary>
    /// Lấy báo cáo doanh thu đa chiều theo khoảng thời gian, cửa hàng, nhân viên
    /// </summary>
    public Task<ApiResponse<RevenueReportResponse>?> GetRevenueReportAsync(
        RevenueReportFilterRequest request,
        CancellationToken ct = default)
    {
        var queryParams = new List<string>();
        if (request.From.HasValue)
            queryParams.Add($"from={Uri.EscapeDataString(request.From.Value.ToString("o"))}");
        if (request.To.HasValue)
            queryParams.Add($"to={Uri.EscapeDataString(request.To.Value.ToString("o"))}");
        if (request.StoreId.HasValue)
            queryParams.Add($"storeId={request.StoreId.Value}");
        if (request.EmployeeId.HasValue)
            queryParams.Add($"employeeId={request.EmployeeId.Value}");

        var queryString = queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : string.Empty;
        var url = $"{ApiRoutes.Reports.Revenue}{queryString}";
        return GetAsync<ApiResponse<RevenueReportResponse>>(url, ct);
    }

    /// <summary>
    /// Lấy báo cáo tồn kho & giá trị tồn
    /// </summary>
    public Task<ApiResponse<InventoryReportResponse>?> GetInventoryReportAsync(
        InventoryReportFilterRequest request,
        CancellationToken ct = default)
    {
        var queryParams = new List<string>();
        if (request.StoreId.HasValue)
            queryParams.Add($"storeId={request.StoreId.Value}");
        if (request.CategoryId.HasValue)
            queryParams.Add($"categoryId={request.CategoryId.Value}");

        var queryString = queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : string.Empty;
        var url = $"{ApiRoutes.Reports.Inventory}{queryString}";
        return GetAsync<ApiResponse<InventoryReportResponse>>(url, ct);
    }

    /// <summary>
    /// Yêu cầu xuất báo cáo ra file Excel hoặc PDF (ClosedXML)
    /// </summary>
    public Task<ApiResponse<ExportReportJobResponse>?> ExportReportAsync(
        ExportReportRequest request,
        CancellationToken ct = default)
    {
        return PostAsync<ApiResponse<ExportReportJobResponse>>(ApiRoutes.Reports.Export, request, ct);
    }

    /// <summary>
    /// Kiểm tra trạng thái tiến trình xuất file
    /// </summary>
    public Task<ApiResponse<ExportJobStatusResponse>?> GetExportJobStatusAsync(
        Guid jobId,
        CancellationToken ct = default)
    {
        return GetAsync<ApiResponse<ExportJobStatusResponse>>(ApiRoutes.Reports.ExportStatus(jobId), ct);
    }

    /// <summary>
    /// Tải trực tiếp file báo cáo đã xuất
    /// </summary>
    public Task<byte[]> DownloadExportReportAsync(
        Guid jobId,
        CancellationToken ct = default)
    {
        return GetByteArrayAsync(ApiRoutes.Reports.ExportDownload(jobId), ct);
    }
}
