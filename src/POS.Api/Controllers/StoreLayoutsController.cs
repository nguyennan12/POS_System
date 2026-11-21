using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using POS.Api.Extensions;
using POS.Api.Mappings;
using POS.Application.UseCases.StoreLayouts;
using POS.Application.UseCases.StoreLayouts.Commands.Cells;
using POS.Application.UseCases.StoreLayouts.Commands.CloneStoreLayout;
using POS.Application.UseCases.StoreLayouts.Commands.CreateStoreLayout;
using POS.Application.UseCases.StoreLayouts.Commands.PublishStoreLayout;
using POS.Application.UseCases.StoreLayouts.Commands.ArchiveStoreLayout;
using POS.Application.UseCases.StoreLayouts.Commands.ShelfLevels;
using POS.Application.UseCases.StoreLayouts.Commands.Shelves;
using POS.Application.UseCases.StoreLayouts.Commands.UpdateStoreLayout;
using POS.Application.UseCases.StoreLayouts.Commands.Zones;
using POS.Application.UseCases.StoreLayouts.Queries.GetActiveStoreLayout;
using POS.Application.UseCases.StoreLayouts.Queries.GetStoreLayoutById;
using POS.Application.UseCases.StoreLayouts.Queries.GetStoreLayouts;
using POS.Application.UseCases.StoreLayouts.Queries.PreviewPickingRoute;
using POS.Contracts.V1.Common;
using POS.Contracts.V1.StoreLayouts;
using POS.Domain.StoreLayouts.Enums;

namespace POS.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/store-layouts")]
public class StoreLayoutsController(ISender mediator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResponse<StoreLayoutSummaryResponse>>>> GetStoreLayouts(
        [FromQuery] Guid? storeId,
        [FromQuery] StoreLayoutStatus? status,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(new GetStoreLayoutsQuery(storeId, status, pageNumber, pageSize), cancellationToken);
        if (result.IsFailure) return this.ToActionResult(result);

        var paged = result.Value!;
        var response = new PagedResponse<StoreLayoutSummaryResponse>(
            paged.Items.Select(i => i.ToResponse()).ToList(),
            paged.TotalCount,
            paged.PageNumber,
            paged.PageSize);

        return Ok(ApiResponse<PagedResponse<StoreLayoutSummaryResponse>>.Ok(response));
    }

    [HttpGet("active")]
    public async Task<ActionResult<ApiResponse<StoreLayoutDetailResponse?>>> GetActive(
        [FromQuery] Guid? storeId,
        CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(new GetActiveStoreLayoutQuery(storeId), cancellationToken);
        if (result.IsFailure) return this.ToActionResult(result);

        return Ok(ApiResponse<StoreLayoutDetailResponse?>.Ok(result.Value?.ToResponse()));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<StoreLayoutDetailResponse>>> GetById(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(new GetStoreLayoutByIdQuery(id), cancellationToken);
        if (result.IsFailure) return this.ToActionResult(result);

        return Ok(ApiResponse<StoreLayoutDetailResponse>.Ok(result.Value!.ToResponse()));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<StoreLayoutDetailResponse>>> Create(
        [FromBody] CreateStoreLayoutRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(new CreateStoreLayoutCommand(request.Name, request.GridWidth, request.GridHeight, request.StoreId), cancellationToken);
        if (result.IsFailure) return this.ToActionResult(result);

        var response = result.Value!.ToResponse();
        return CreatedAtAction(nameof(GetById), new { id = response.Id }, ApiResponse<StoreLayoutDetailResponse>.Ok(response));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<StoreLayoutDetailResponse>>> Update(
        Guid id,
        [FromBody] UpdateStoreLayoutRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(new UpdateStoreLayoutCommand(id, request.Name, request.ExpectedRevision), cancellationToken);
        if (result.IsFailure) return this.ToActionResult(result);

        return Ok(ApiResponse<StoreLayoutDetailResponse>.Ok(result.Value!.ToResponse()));
    }

    [HttpPost("{id:guid}/clone")]
    public async Task<ActionResult<ApiResponse<CloneStoreLayoutResponse>>> Clone(
        Guid id,
        [FromBody] CloneStoreLayoutRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(new CloneStoreLayoutCommand(id, request.TargetStoreId, request.NewName, request.CopyPlacements, request.ExpectedRevision), cancellationToken);
        if (result.IsFailure) return this.ToActionResult(result);

        return Ok(ApiResponse<CloneStoreLayoutResponse>.Ok(result.Value!.ToResponse()));
    }

    [HttpPost("{id:guid}/publish")]
    public async Task<ActionResult<ApiResponse<StoreLayoutDetailResponse>>> Publish(
        Guid id,
        [FromBody] PublishStoreLayoutRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(new PublishStoreLayoutCommand(id, request.ExpectedRevision), cancellationToken);
        if (result.IsFailure) return this.ToActionResult(result);

        return Ok(ApiResponse<StoreLayoutDetailResponse>.Ok(result.Value!.ToResponse()));
    }

    [HttpPost("{id:guid}/archive")]
    public async Task<ActionResult<ApiResponse<StoreLayoutDetailResponse>>> Archive(
        Guid id,
        [FromBody] ArchiveStoreLayoutRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(new ArchiveStoreLayoutCommand(id, request.ExpectedRevision), cancellationToken);
        if (result.IsFailure) return this.ToActionResult(result);

        return Ok(ApiResponse<StoreLayoutDetailResponse>.Ok(result.Value!.ToResponse()));
    }

    [HttpPut("{id:guid}/cells")]
    public async Task<ActionResult<ApiResponse<StoreLayoutDetailResponse>>> UpdateCells(
        Guid id,
        [FromBody] UpdateLayoutCellsRequest request,
        CancellationToken cancellationToken = default)
    {
        var cellDtos = request.Cells.Select(c => new LayoutCellChangeDto(
            c.X,
            c.Y,
            Enum.TryParse<LayoutCellType>(c.CellType, true, out var ct) ? ct : LayoutCellType.Walkway,
            c.TraversalCost)).ToList();

        var result = await mediator.Send(new UpdateLayoutCellsCommand(id, cellDtos, request.ExpectedRevision), cancellationToken);
        if (result.IsFailure) return this.ToActionResult(result);

        return Ok(ApiResponse<StoreLayoutDetailResponse>.Ok(result.Value!.ToResponse()));
    }

    [HttpPost("{id:guid}/zones")]
    public async Task<ActionResult<ApiResponse<StoreZoneResponse>>> CreateZone(
        Guid id,
        [FromBody] CreateZoneRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(new CreateZoneCommand(id, request.Code, request.Name, request.Color, request.PositionX, request.PositionY, request.Width, request.Height, request.ExpectedRevision), cancellationToken);
        if (result.IsFailure) return this.ToActionResult(result);

        return StatusCode(StatusCodes.Status201Created, ApiResponse<StoreZoneResponse>.Ok(result.Value!.ToResponse()));
    }

    [HttpPut("{id:guid}/zones/{zoneId:guid}")]
    public async Task<ActionResult<ApiResponse<StoreZoneResponse>>> UpdateZone(
        Guid id,
        Guid zoneId,
        [FromBody] UpdateZoneRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(new UpdateZoneCommand(id, zoneId, request.Code, request.Name, request.Color, request.PositionX, request.PositionY, request.Width, request.Height, request.ExpectedRevision), cancellationToken);
        if (result.IsFailure) return this.ToActionResult(result);

        return Ok(ApiResponse<StoreZoneResponse>.Ok(result.Value!.ToResponse()));
    }

    [HttpDelete("{id:guid}/zones/{zoneId:guid}")]
    public async Task<ActionResult<ApiResponse<LayoutMutationResponse>>> DeleteZone(
        Guid id,
        Guid zoneId,
        [FromQuery] int expectedRevision,
        CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(new DeleteZoneCommand(id, zoneId, expectedRevision), cancellationToken);
        if (result.IsFailure) return this.ToActionResult(result);

        return Ok(ApiResponse<LayoutMutationResponse>.Ok(new LayoutMutationResponse(result.Value!.LayoutId, result.Value!.NewRevision)));
    }

    [HttpPost("{id:guid}/shelves")]
    public async Task<ActionResult<ApiResponse<ShelfResponse>>> CreateShelf(
        Guid id,
        [FromBody] CreateShelfRequest request,
        CancellationToken cancellationToken = default)
    {
        var orientation = Enum.TryParse<ShelfOrientation>(request.Orientation, true, out var ori) ? ori : ShelfOrientation.North;
        var result = await mediator.Send(new CreateShelfCommand(id, request.ZoneId, request.Code, request.Name, request.PositionX, request.PositionY, request.Width, request.Height, orientation, request.ExpectedRevision), cancellationToken);
        if (result.IsFailure) return this.ToActionResult(result);

        return StatusCode(StatusCodes.Status201Created, ApiResponse<ShelfResponse>.Ok(result.Value!.ToResponse()));
    }

    [HttpPut("{id:guid}/shelves/{shelfId:guid}")]
    public async Task<ActionResult<ApiResponse<ShelfResponse>>> UpdateShelf(
        Guid id,
        Guid shelfId,
        [FromBody] UpdateShelfRequest request,
        CancellationToken cancellationToken = default)
    {
        var orientation = Enum.TryParse<ShelfOrientation>(request.Orientation, true, out var ori) ? ori : ShelfOrientation.North;
        var status = Enum.TryParse<ShelfStatus>(request.Status, true, out var st) ? st : ShelfStatus.Active;

        var result = await mediator.Send(new UpdateShelfCommand(id, shelfId, request.ZoneId, request.Code, request.Name, request.PositionX, request.PositionY, request.Width, request.Height, orientation, status, request.ExpectedRevision), cancellationToken);
        if (result.IsFailure) return this.ToActionResult(result);

        return Ok(ApiResponse<ShelfResponse>.Ok(result.Value!.ToResponse()));
    }

    [HttpDelete("{id:guid}/shelves/{shelfId:guid}")]
    public async Task<ActionResult<ApiResponse<LayoutMutationResponse>>> DeleteShelf(
        Guid id,
        Guid shelfId,
        [FromQuery] int expectedRevision,
        CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(new DeleteShelfCommand(id, shelfId, expectedRevision), cancellationToken);
        if (result.IsFailure) return this.ToActionResult(result);

        return Ok(ApiResponse<LayoutMutationResponse>.Ok(new LayoutMutationResponse(result.Value!.LayoutId, result.Value!.NewRevision)));
    }

    [HttpPost("{id:guid}/shelves/{shelfId:guid}/levels")]
    public async Task<ActionResult<ApiResponse<ShelfLevelResponse>>> AddShelfLevel(
        Guid id,
        Guid shelfId,
        [FromBody] AddShelfLevelRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(new AddShelfLevelCommand(id, shelfId, request.LevelNumber, request.Label, request.SortOrder, request.ExpectedRevision), cancellationToken);
        if (result.IsFailure) return this.ToActionResult(result);

        return StatusCode(StatusCodes.Status201Created, ApiResponse<ShelfLevelResponse>.Ok(result.Value!.ToResponse()));
    }

    [HttpPut("{id:guid}/shelves/{shelfId:guid}/levels/{levelId:guid}")]
    public async Task<ActionResult<ApiResponse<ShelfLevelResponse>>> UpdateShelfLevel(
        Guid id,
        Guid shelfId,
        Guid levelId,
        [FromBody] UpdateShelfLevelRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(new UpdateShelfLevelCommand(id, shelfId, levelId, request.Label, request.SortOrder, request.ExpectedRevision), cancellationToken);
        if (result.IsFailure) return this.ToActionResult(result);

        return Ok(ApiResponse<ShelfLevelResponse>.Ok(result.Value!.ToResponse()));
    }

    [HttpDelete("{id:guid}/shelves/{shelfId:guid}/levels/{levelId:guid}")]
    public async Task<ActionResult<ApiResponse<LayoutMutationResponse>>> DeleteShelfLevel(
        Guid id,
        Guid shelfId,
        Guid levelId,
        [FromQuery] int expectedRevision,
        CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(new DeleteShelfLevelCommand(id, shelfId, levelId, expectedRevision), cancellationToken);
        if (result.IsFailure) return this.ToActionResult(result);

        return Ok(ApiResponse<LayoutMutationResponse>.Ok(new LayoutMutationResponse(result.Value!.LayoutId, result.Value!.NewRevision)));
    }

    [HttpPost("active/route-preview")]
    public async Task<ActionResult<ApiResponse<PickingRoutePreviewResponse>>> PreviewPickingRoute(
        [FromBody] PreviewPickingRouteRequest request,
        CancellationToken cancellationToken = default)
    {
        var items = request.Items.Select(i => new PickingRouteItemDto(i.SkuId, i.Quantity)).ToList();
        var startPoint = request.StartCell != null ? new LayoutPointDto(request.StartCell.X, request.StartCell.Y) : null;

        var result = await mediator.Send(new PreviewPickingRouteQuery(items, request.StoreId, startPoint), cancellationToken);
        if (result.IsFailure) return this.ToActionResult(result);

        return Ok(ApiResponse<PickingRoutePreviewResponse>.Ok(result.Value!.ToResponse()));
    }

    [HttpPost("{id:guid}/route-preview")]
    public async Task<ActionResult<ApiResponse<PickingRoutePreviewResponse>>> PreviewPickingRouteForLayout(
        [FromRoute] Guid id,
        [FromBody] PreviewPickingRouteRequest request,
        CancellationToken cancellationToken = default)
    {
        var items = request.Items.Select(i => new PickingRouteItemDto(i.SkuId, i.Quantity)).ToList();
        var startPoint = request.StartCell != null ? new LayoutPointDto(request.StartCell.X, request.StartCell.Y) : null;

        var result = await mediator.Send(new PreviewPickingRouteQuery(items, request.StoreId, startPoint, id), cancellationToken);
        if (result.IsFailure) return this.ToActionResult(result);

        return Ok(ApiResponse<PickingRoutePreviewResponse>.Ok(result.Value!.ToResponse()));
    }
}
