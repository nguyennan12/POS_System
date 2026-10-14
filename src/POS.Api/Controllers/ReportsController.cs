using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using POS.Api.Extensions;
using POS.Application.UseCases.Reports.Commands.ExportReport;
using POS.Application.UseCases.Reports.Queries.GetDashboardSummary;
using POS.Application.UseCases.Reports.Queries.GetExportJobStatus;
using POS.Application.UseCases.Reports.Queries.GetExportReportFile;
using POS.Application.UseCases.Reports.Queries.GetInventoryReport;
using POS.Application.UseCases.Reports.Queries.GetRevenueReport;
using POS.Application.UseCases.Reports.Queries.GetShiftReport;
using POS.Application.UseCases.Reports.Queries.GetSlowMovingReport;
using POS.Application.UseCases.Reports.Queries.GetTopSellingReport;
using POS.Contracts.V1.Common;
using POS.Contracts.V1.Reports;

namespace POS.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/reports")]
public class ReportsController(ISender mediator) : ControllerBase
{
    /// <summary>
    /// [T56] Báo cáo doanh thu đa chiều (theo thời gian, cửa hàng, nhân viên)
    /// </summary>
    [HttpGet("revenue")]
    public async Task<ActionResult<ApiResponse<RevenueReportResponse>>> GetRevenueReport(
        [FromQuery] RevenueReportFilterRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new GetRevenueReportQuery(request.From, request.To, request.StoreId, request.EmployeeId),
            cancellationToken);

        return this.ToActionResult(result);
    }

    /// <summary>
    /// [T56] Báo cáo top sản phẩm bán chạy nhất
    /// </summary>
    [HttpGet("top-selling")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<TopSellingProductItem>>>> GetTopSellingReport(
        [FromQuery] TopSellingReportFilterRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new GetTopSellingReportQuery(request.From, request.To, request.Limit),
            cancellationToken);

        return this.ToActionResult(result);
    }

    /// <summary>
    /// [T56] Báo cáo tồn kho theo danh mục hoặc toàn cửa hàng
    /// </summary>
    [HttpGet("inventory")]
    public async Task<ActionResult<ApiResponse<InventoryReportResponse>>> GetInventoryReport(
        [FromQuery] InventoryReportFilterRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new GetInventoryReportQuery(request.StoreId, request.CategoryId),
            cancellationToken);

        return this.ToActionResult(result);
    }

    /// <summary>
    /// [T56] Báo cáo danh sách hàng tồn chậm luân chuyển (không phát sinh bán trong N ngày)
    /// </summary>
    [HttpGet("slow-moving")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<SlowMovingItemResponse>>>> GetSlowMovingReport(
        [FromQuery] int days = 30,
        CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(
            new GetSlowMovingReportQuery(days),
            cancellationToken);

        return this.ToActionResult(result);
    }

    /// <summary>
    /// [T56] Báo cáo tổng kết ca làm việc
    /// </summary>
    [HttpGet("shift/{id:guid}")]
    public async Task<ActionResult<ApiResponse<ShiftReportResponse>>> GetShiftReport(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new GetShiftReportQuery(id),
            cancellationToken);

        return this.ToActionResult(result);
    }

    /// <summary>
    /// [T56] Tổng quan Dashboard kinh doanh thời gian thực
    /// </summary>
    [HttpGet("dashboard")]
    public async Task<ActionResult<ApiResponse<DashboardSummaryResponse>>> GetDashboardSummary(
        [FromQuery] Guid? storeId,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new GetDashboardSummaryQuery(storeId),
            cancellationToken);

        return this.ToActionResult(result);
    }

    /// <summary>
    /// [T56] Xuất dữ liệu báo cáo ra file Excel (ClosedXML) hoặc PDF
    /// </summary>
    [HttpPost("export")]
    public async Task<ActionResult<ApiResponse<ExportReportJobResponse>>> ExportReport(
        [FromBody] ExportReportRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new ExportReportCommand(request.Type, request.Format, request.Params),
            cancellationToken);

        return this.ToActionResult(result);
    }

    /// <summary>
    /// [T56] Kiểm tra trạng thái xuất báo cáo (hoặc tải trực tiếp nếu download=true)
    /// </summary>
    [HttpGet("export/{jobId:guid}")]
    public async Task<IActionResult> GetExportJobStatus(
        [FromRoute] Guid jobId,
        [FromQuery] bool download = false,
        CancellationToken cancellationToken = default)
    {
        if (download)
        {
            var fileResult = await mediator.Send(new GetExportReportFileQuery(jobId), cancellationToken);
            if (fileResult.IsFailure)
            {
                return this.ToActionResult(fileResult);
            }

            return File(fileResult.Value!.FileBytes, fileResult.Value.ContentType, fileResult.Value.FileName);
        }

        var statusResult = await mediator.Send(new GetExportJobStatusQuery(jobId), cancellationToken);
        return this.ToActionResult(statusResult);
    }

    /// <summary>
    /// [T56] Tải file báo cáo đã xuất xong
    /// </summary>
    [HttpGet("export/{jobId:guid}/download")]
    public async Task<IActionResult> DownloadExportReport(
        [FromRoute] Guid jobId,
        CancellationToken cancellationToken)
    {
        var fileResult = await mediator.Send(new GetExportReportFileQuery(jobId), cancellationToken);
        if (fileResult.IsFailure)
        {
            return this.ToActionResult(fileResult);
        }

        return File(fileResult.Value!.FileBytes, fileResult.Value.ContentType, fileResult.Value.FileName);
    }
}
