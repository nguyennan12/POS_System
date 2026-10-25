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
    /// [T56] BÃ¡o cÃ¡o doanh thu Ä‘a chiá»u (theo thá»i gian, cá»­a hÃ ng, nhÃ¢n viÃªn)
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
    /// [T56] BÃ¡o cÃ¡o top sáº£n pháº©m bÃ¡n cháº¡y nháº¥t
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
    /// [T56] BÃ¡o cÃ¡o tá»“n kho theo danh má»¥c hoáº·c toÃ n cá»­a hÃ ng
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
    /// [T56] BÃ¡o cÃ¡o danh sÃ¡ch hÃ ng tá»“n cháº­m luÃ¢n chuyá»ƒn (khÃ´ng phÃ¡t sinh bÃ¡n trong N ngÃ y)
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
    /// [T56] BÃ¡o cÃ¡o tá»•ng káº¿t ca lÃ m viá»‡c
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
    /// [T56] Tá»•ng quan Dashboard kinh doanh thá»i gian thá»±c
    /// <summary>
    /// [T55] Tổng quan Dashboard kinh doanh thời gian thực
    /// </summary>
    [HttpGet("dashboard")]
    public async Task<ActionResult<ApiResponse<DashboardSummaryResponse>>> GetDashboardSummary(
        [FromQuery] DashboardFilterRequest? request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new GetDashboardSummaryQuery(request?.StoreId, request?.Days ?? 7),
            cancellationToken);

        return this.ToActionResult(result);
    }
    /// <summary>
    /// [T56] Xuáº¥t dá»¯ liá»‡u bÃ¡o cÃ¡o ra file Excel (ClosedXML) hoáº·c PDF
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
    /// [T56] Kiá»ƒm tra tráº¡ng thÃ¡i xuáº¥t bÃ¡o cÃ¡o (hoáº·c táº£i trá»±c tiáº¿p náº¿u download=true)
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
    /// [T56] Táº£i file bÃ¡o cÃ¡o Ä‘Ã£ xuáº¥t xong
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
