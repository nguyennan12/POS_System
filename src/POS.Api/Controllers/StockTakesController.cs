using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using POS.Api.Extensions;
using POS.Api.Mappings;
using POS.Application.UseCases.Inventory.StockTake.Commands.ApproveStockTake;
using POS.Application.UseCases.Inventory.StockTake.Commands.CreateStockTake;
using POS.Application.UseCases.Inventory.StockTake.Commands.SubmitStockTake;
using POS.Application.UseCases.Inventory.StockTake.Commands.UpdateStockTakeItems;
using POS.Application.UseCases.Inventory.StockTake.Queries.GetStockTakeById;
using POS.Application.UseCases.Inventory.StockTake.Queries.GetStockTakes;
using POS.Contracts.V1.Common;
using POS.Contracts.V1.Inventory;

namespace POS.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/stock-takes")]
[Route("api/v1/inventory/stock-takes")]
public class StockTakesController(ISender mediator) : ControllerBase
{
    /// <summary>
    /// [T32] Lấy danh sách phiếu kiểm kê kho phân trang.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResponse<StockTakeSummaryResponse>>>> GetStockTakes(
        [FromQuery] StockTakeFilterRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new GetStockTakesQuery(
                request.StoreId,
                request.Status,
                request.From,
                request.To,
                request.PageNumber,
                request.PageSize),
            cancellationToken);

        if (result.IsFailure) return this.ToActionResult(result);

        var paged = result.Value!;
        return Ok(ApiResponse<PagedResponse<StockTakeSummaryResponse>>.Ok(
            new PagedResponse<StockTakeSummaryResponse>(
                paged.Items.Select(x => x.ToResponse()).ToList().AsReadOnly(),
                paged.PageNumber,
                paged.PageSize,
                paged.TotalCount)));
    }

    /// <summary>
    /// [T32] Lấy chi tiết phiếu kiểm kê kho theo ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<StockTakeDetailResponse>>> GetStockTakeById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetStockTakeByIdQuery(id), cancellationToken);

        if (result.IsFailure) return this.ToActionResult(result);
        return Ok(ApiResponse<StockTakeDetailResponse>.Ok(result.Value!.ToResponse()));
    }

    /// <summary>
    /// [T32] Tạo phiếu kiểm kê mới (trạng thái Draft).
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<StockTakeDetailResponse>>> CreateStockTake(
        [FromBody] CreateStockTakeRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new CreateStockTakeCommand(request.Note),
            cancellationToken);

        if (result.IsFailure) return this.ToActionResult(result);

        return CreatedAtAction(
            nameof(GetStockTakeById),
            new { id = result.Value!.Id },
            ApiResponse<StockTakeDetailResponse>.Ok(result.Value!.ToResponse()));
    }

    /// <summary>
    /// [T32] Cập nhật số lượng đếm thực tế của các mặt hàng trong phiếu kiểm kê.
    /// Tự động tính diff_qty = actual_qty - system_qty.
    /// </summary>
    [HttpPut("{id:guid}/items")]
    public async Task<ActionResult<ApiResponse<StockTakeDetailResponse>>> UpdateStockTakeItems(
        Guid id,
        [FromBody] UpdateStockTakeItemsRequest request,
        CancellationToken cancellationToken)
    {
        var items = request.Items
            .Select(i => new StockTakeItemInput(i.SkuId, i.ActualQty, i.Note))
            .ToList()
            .AsReadOnly();

        var result = await mediator.Send(
            new UpdateStockTakeItemsCommand(id, items),
            cancellationToken);

        if (result.IsFailure) return this.ToActionResult(result);
        return Ok(ApiResponse<StockTakeDetailResponse>.Ok(result.Value!.ToResponse()));
    }

    /// <summary>
    /// [T32] Gửi phiếu kiểm kê để chờ duyệt (Draft -> Pending).
    /// </summary>
    [HttpPost("{id:guid}/submit")]
    public async Task<ActionResult<ApiResponse<StockTakeDetailResponse>>> SubmitStockTake(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new SubmitStockTakeCommand(id), cancellationToken);

        if (result.IsFailure) return this.ToActionResult(result);
        return Ok(ApiResponse<StockTakeDetailResponse>.Ok(result.Value!.ToResponse()));
    }

    /// <summary>
    /// [T32] Quản lý phê duyệt chênh lệch kiểm kê và tự động cân bằng tồn kho (Adjust).
    /// </summary>
    [HttpPost("{id:guid}/approve")]
    public async Task<ActionResult<ApiResponse<StockTakeDetailResponse>>> ApproveStockTake(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new ApproveStockTakeCommand(id), cancellationToken);

        if (result.IsFailure) return this.ToActionResult(result);
        return Ok(ApiResponse<StockTakeDetailResponse>.Ok(result.Value!.ToResponse()));
    }
}
