using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using POS.Api.Extensions;
using POS.Api.Mappings;
using POS.Application.UseCases.Inventory.Stock.Commands.DisposeStock;
using POS.Application.UseCases.Inventory.Stock.Queries.GetInventorySummary;
using POS.Application.UseCases.Inventory.Stock.Queries.GetStockAlerts;
using POS.Application.UseCases.Inventory.Stock.Queries.GetStockBatches;
using POS.Application.UseCases.Inventory.Stock.Queries.GetStockTransactions;
using POS.Application.UseCases.Inventory.StockIn.Commands.CompleteStockInVoucher;
using POS.Application.UseCases.Inventory.StockIn.Commands.CreateStockInVoucher;
using POS.Application.UseCases.Inventory.StockIn.Queries.GetStockInVoucherById;
using POS.Application.UseCases.Inventory.StockIn.Queries.GetStockInVouchers;
using POS.Contracts.V1.Common;
using POS.Contracts.V1.Inventory;

namespace POS.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/inventory")]
public class InventoryController(ISender mediator) : ControllerBase
{
    // ── Stock-In Vouchers ─────────────────────────────────────────────────────

    /// <summary>
    /// [T30] Lấy danh sách phiếu nhập kho phân trang.
    /// </summary>
    [HttpGet("stock-in")]
    public async Task<ActionResult<ApiResponse<PagedResponse<StockInVoucherSummaryResponse>>>> GetStockInVouchers(
        [FromQuery] StockInVoucherFilterRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new GetStockInVouchersQuery(
                request.SupplierId,
                request.Status,
                request.From,
                request.To,
                request.PageNumber,
                request.PageSize),
            cancellationToken);

        if (result.IsFailure) return this.ToActionResult(result);

        var paged = result.Value!;
        return Ok(ApiResponse<PagedResponse<StockInVoucherSummaryResponse>>.Ok(
            new PagedResponse<StockInVoucherSummaryResponse>(
                paged.Items.Select(v => v.ToResponse()).ToList().AsReadOnly(),
                paged.PageNumber,
                paged.PageSize,
                paged.TotalCount)));
    }

    /// <summary>
    /// [T30] Lấy chi tiết phiếu nhập kho theo ID.
    /// </summary>
    [HttpGet("stock-in/{id:guid}")]
    public async Task<ActionResult<ApiResponse<StockInVoucherDetailResponse>>> GetStockInVoucherById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetStockInVoucherByIdQuery(id), cancellationToken);

        if (result.IsFailure) return this.ToActionResult(result);
        return Ok(ApiResponse<StockInVoucherDetailResponse>.Ok(result.Value!.ToResponse()));
    }

    /// <summary>
    /// [T30] Tạo phiếu nhập kho mới (trạng thái Draft).
    /// </summary>
    [HttpPost("stock-in")]
    public async Task<ActionResult<ApiResponse<StockInVoucherDetailResponse>>> CreateStockInVoucher(
        [FromBody] CreateStockInVoucherRequest request,
        CancellationToken cancellationToken)
    {
        var items = request.Items
            .Select(i => new StockInItemInput(i.SkuId, i.Qty, i.UnitPrice))
            .ToList()
            .AsReadOnly();

        var result = await mediator.Send(
            new CreateStockInVoucherCommand(request.SupplierId, items, request.Note),
            cancellationToken);

        if (result.IsFailure) return this.ToActionResult(result);

        return CreatedAtAction(
            nameof(GetStockInVoucherById),
            new { id = result.Value!.Id },
            ApiResponse<StockInVoucherDetailResponse>.Ok(result.Value!.ToResponse()));
    }

    /// <summary>
    /// [T30] Hoàn thành phiếu nhập: tăng tồn kho + tạo StockTransaction StockIn + tính giá vốn bình quân.
    /// </summary>
    [HttpPost("stock-in/{id:guid}/complete")]
    public async Task<ActionResult<ApiResponse<StockInVoucherDetailResponse>>> CompleteStockInVoucher(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new CompleteStockInVoucherCommand(id), cancellationToken);

        if (result.IsFailure) return this.ToActionResult(result);
        return Ok(ApiResponse<StockInVoucherDetailResponse>.Ok(result.Value!.ToResponse()));
    }

    // ── Inventory Summary (T31) ───────────────────────────────────────────────

    /// <summary>
    /// [T31] Lấy tồn kho hiện tại phân trang với bộ lọc SKU/Category/Search.
    /// </summary>
    [HttpGet("stock")]
    public async Task<ActionResult<ApiResponse<PagedResponse<StockEntryResponse>>>> GetInventorySummary(
        [FromQuery] InventoryFilterRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new GetInventorySummaryQuery(
                request.SkuId,
                request.CategoryId,
                request.Search,
                request.PageNumber,
                request.PageSize),
            cancellationToken);

        if (result.IsFailure) return this.ToActionResult(result);

        var paged = result.Value!;
        return Ok(ApiResponse<PagedResponse<StockEntryResponse>>.Ok(
            new PagedResponse<StockEntryResponse>(
                paged.Items.Select(e => e.ToResponse()).ToList().AsReadOnly(),
                paged.PageNumber,
                paged.PageSize,
                paged.TotalCount)));
    }

    /// <summary>
    /// [T31] Lấy danh sách cảnh báo tồn kho: min-stock và hàng cận ngày hết hạn.
    /// </summary>
    [HttpGet("stock/alerts")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<StockAlertResponse>>>> GetStockAlerts(
        [FromQuery] int nearExpiryDays = 30,
        CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(new GetStockAlertsQuery(nearExpiryDays), cancellationToken);

        if (result.IsFailure) return this.ToActionResult(result);
        return Ok(ApiResponse<IReadOnlyList<StockAlertResponse>>.Ok(
            result.Value!.Select(a => a.ToResponse()).ToList().AsReadOnly()));
    }

    /// <summary>
    /// [T31] Lấy danh sách lô hàng (StockBatch) với bộ lọc theo SKU và hạn dùng.
    /// </summary>
    [HttpGet("stock/batches")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<StockBatchResponse>>>> GetStockBatches(
        [FromQuery] BatchFilterRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new GetStockBatchesQuery(request.SkuId, request.ExpiryBefore),
            cancellationToken);

        if (result.IsFailure) return this.ToActionResult(result);
        return Ok(ApiResponse<IReadOnlyList<StockBatchResponse>>.Ok(
            result.Value!.Select(b => b.ToResponse()).ToList().AsReadOnly()));
    }

    /// <summary>
    /// [T31] Lấy lịch sử giao dịch kho phân trang.
    /// </summary>
    [HttpGet("stock/transactions")]
    public async Task<ActionResult<ApiResponse<PagedResponse<StockTransactionResponse>>>> GetStockTransactions(
        [FromQuery] Guid? skuId,
        [FromQuery] string? type,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(
            new GetStockTransactionsQuery(skuId, type, pageNumber, pageSize),
            cancellationToken);

        if (result.IsFailure) return this.ToActionResult(result);

        var paged = result.Value!;
        return Ok(ApiResponse<PagedResponse<StockTransactionResponse>>.Ok(
            new PagedResponse<StockTransactionResponse>(
                paged.Items.Select(t => t.ToResponse()).ToList().AsReadOnly(),
                paged.PageNumber,
                paged.PageSize,
                paged.TotalCount)));
    }

    /// <summary>
    /// [T31] Xuất hủy hàng hóa (Dispose), trừ kho và ghi StockTransaction Dispose.
    /// </summary>
    [HttpPost("stock/dispose")]
    public async Task<ActionResult<ApiResponse<StockTransactionResponse>>> DisposeStock(
        [FromBody] DisposeStockRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new DisposeStockCommand(request.SkuId, request.Qty, request.Note),
            cancellationToken);

        if (result.IsFailure) return this.ToActionResult(result);
        return Ok(ApiResponse<StockTransactionResponse>.Ok(result.Value!.ToResponse()));
    }
}
