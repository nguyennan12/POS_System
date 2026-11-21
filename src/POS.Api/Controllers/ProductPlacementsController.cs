using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using POS.Api.Extensions;
using POS.Api.Mappings;
using POS.Application.UseCases.StoreLayouts.Commands.Placements;
using POS.Application.UseCases.StoreLayouts.Queries.GetPlacementHistory;
using POS.Application.UseCases.StoreLayouts.Queries.GetProductLocations;
using POS.Application.UseCases.StoreLayouts.Queries.GetShelfContents;
using POS.Contracts.V1.Common;
using POS.Contracts.V1.StoreLayouts;

namespace POS.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/product-placements")]
public class ProductPlacementsController(ISender mediator) : ControllerBase
{
    [HttpGet("by-sku/{skuId:guid}")]
    public async Task<ActionResult<ApiResponse<ProductLocationsResponse>>> GetLocationsBySku(
        Guid skuId,
        [FromQuery] Guid? storeId,
        CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(new GetProductLocationsQuery(skuId, storeId), cancellationToken);
        if (result.IsFailure) return this.ToActionResult(result);

        return Ok(ApiResponse<ProductLocationsResponse>.Ok(result.Value!.ToResponse()));
    }

    [HttpGet("shelves/{shelfId:guid}/contents")]
    public async Task<ActionResult<ApiResponse<ShelfContentsResponse>>> GetShelfContents(
        Guid shelfId,
        [FromQuery] Guid? storeId,
        CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(new GetShelfContentsQuery(shelfId, storeId), cancellationToken);
        if (result.IsFailure) return this.ToActionResult(result);

        return Ok(ApiResponse<ShelfContentsResponse>.Ok(result.Value!.ToResponse()));
    }

    [HttpGet("history/{skuId:guid}")]
    public async Task<ActionResult<ApiResponse<PagedResponse<PlacementHistoryResponse>>>> GetPlacementHistory(
        Guid skuId,
        [FromQuery] Guid? storeId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(new GetPlacementHistoryQuery(skuId, storeId, pageNumber, pageSize), cancellationToken);
        if (result.IsFailure) return this.ToActionResult(result);

        var paged = result.Value!;
        var response = new PagedResponse<PlacementHistoryResponse>(
            paged.Items.Select(i => i.ToResponse()).ToList(),
            paged.TotalCount,
            paged.PageNumber,
            paged.PageSize);

        return Ok(ApiResponse<PagedResponse<PlacementHistoryResponse>>.Ok(response));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<ProductPlacementResponse>>> Assign(
        [FromBody] AssignProductPlacementRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(new AssignProductPlacementCommand(request.LayoutId, request.ShelfLevelId, request.SkuId, request.IsPrimary, request.Note, request.ExpectedRevision), cancellationToken);
        if (result.IsFailure) return this.ToActionResult(result);

        return StatusCode(StatusCodes.Status201Created, ApiResponse<ProductPlacementResponse>.Ok(result.Value!.ToResponse()));
    }

    [HttpPost("{id:guid}/move")]
    public async Task<ActionResult<ApiResponse<ProductPlacementResponse>>> Move(
        Guid id,
        [FromBody] MoveProductPlacementRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(new MoveProductPlacementCommand(request.LayoutId, id, request.TargetShelfLevelId, request.Note, request.ExpectedRevision), cancellationToken);
        if (result.IsFailure) return this.ToActionResult(result);

        return Ok(ApiResponse<ProductPlacementResponse>.Ok(result.Value!.ToResponse()));
    }

    [HttpPut("{id:guid}/primary")]
    public async Task<ActionResult<ApiResponse<ProductPlacementResponse>>> SetPrimary(
        Guid id,
        [FromBody] SetPrimaryPlacementRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(new SetPrimaryPlacementCommand(request.LayoutId, id, request.ExpectedRevision), cancellationToken);
        if (result.IsFailure) return this.ToActionResult(result);

        return Ok(ApiResponse<ProductPlacementResponse>.Ok(result.Value!.ToResponse()));
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<LayoutMutationResponse>>> Remove(
        Guid id,
        [FromQuery] Guid layoutId,
        [FromQuery] int expectedRevision,
        [FromQuery] string? note,
        CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(new RemoveProductPlacementCommand(layoutId, id, note, expectedRevision), cancellationToken);
        if (result.IsFailure) return this.ToActionResult(result);

        return Ok(ApiResponse<LayoutMutationResponse>.Ok(new LayoutMutationResponse(result.Value!.LayoutId, result.Value!.NewRevision)));
    }
}
