using POS.Application.UseCases.StoreLayouts;
using POS.Contracts.V1.StoreLayouts;

namespace POS.Api.Mappings;

public static class StoreLayoutContractMappings
{
    public static StoreLayoutSummaryResponse ToResponse(this StoreLayoutSummaryDto dto) =>
        new(dto.Id, dto.StoreId, dto.VersionNumber, dto.Name, dto.GridWidth, dto.GridHeight, dto.Status.ToString(), dto.Revision, dto.CreatedAt, dto.PublishedAt);

    public static StoreZoneResponse ToResponse(this StoreZoneDto dto) =>
        new(dto.Id, dto.LayoutId, dto.Code, dto.Name, dto.Color, dto.PositionX, dto.PositionY, dto.Width, dto.Height);

    public static LayoutCellResponse ToResponse(this StoreLayoutCellDto dto) =>
        new(dto.Id, dto.X, dto.Y, dto.CellType.ToString(), dto.TraversalCost);

    public static ShelfLevelResponse ToResponse(this ShelfLevelDto dto) =>
        new(dto.Id, dto.ShelfId, dto.LevelNumber, dto.Label, dto.SortOrder, dto.Placements?.Select(p => p.ToResponse()).ToList());

    public static ShelfResponse ToResponse(this ShelfDto dto) =>
        new(dto.Id, dto.LayoutId, dto.ZoneId, dto.Code, dto.Name, dto.PositionX, dto.PositionY, dto.Width, dto.Height, dto.Orientation.ToString(), dto.Status.ToString(), dto.Levels.Select(l => l.ToResponse()).ToList());

    public static StoreLayoutDetailResponse ToResponse(this StoreLayoutDetailDto dto) =>
        new(dto.Id, dto.StoreId, dto.VersionNumber, dto.Name, dto.GridWidth, dto.GridHeight, dto.Status.ToString(), dto.Revision, dto.CreatedBy, dto.CreatedAt, dto.PublishedBy, dto.PublishedAt,
            dto.Zones.Select(z => z.ToResponse()).ToList(),
            dto.Cells.Select(c => c.ToResponse()).ToList(),
            dto.Shelves.Select(s => s.ToResponse()).ToList());

    public static ProductPlacementResponse ToResponse(this ProductPlacementDto dto) =>
        new(dto.Id, dto.ShelfLevelId, dto.SkuId, dto.IsPrimary, dto.Note, dto.UpdatedBy, dto.UpdatedAt, dto.LayoutRevision);

    public static ProductLocationItemResponse ToResponse(this ProductLocationDto dto) =>
        new(dto.ZoneId, dto.ZoneName, dto.ShelfId, dto.ShelfCode, dto.ShelfName, dto.ShelfLevelId, dto.LevelNumber, dto.IsPrimary, dto.PickCellX, dto.PickCellY);

    public static ProductLocationsResponse ToResponse(this ProductLocationsDto dto) =>
        new(dto.SkuId, dto.LayoutId, dto.LayoutVersion, dto.HasPlacement, dto.Locations.Select(l => l.ToResponse()).ToList());

    public static ShelfSkuContentResponse ToResponse(this ShelfSkuContentDto dto) =>
        new(dto.PlacementId, dto.SkuId, dto.SkuCode, dto.ProductName, dto.Barcode, dto.IsPrimary, dto.Note);

    public static ShelfLevelContentsResponse ToResponse(this ShelfLevelContentsDto dto) =>
        new(dto.LevelId, dto.LevelNumber, dto.Label, dto.SkuPlacements.Select(p => p.ToResponse()).ToList());

    public static ShelfContentsResponse ToResponse(this ShelfContentsDto dto) =>
        new(dto.ShelfId, dto.ShelfCode, dto.ShelfName, dto.Levels.Select(l => l.ToResponse()).ToList());

    public static PlacementHistoryResponse ToResponse(this PlacementHistoryDto dto) =>
        new(dto.Id, dto.SkuId, dto.Action.ToString(), dto.FromShelfLevelId, dto.ToShelfLevelId, dto.PerformedBy, dto.Note, dto.CreatedAt);

    public static CloneStoreLayoutResponse ToResponse(this CloneStoreLayoutResultDto dto) =>
        new(dto.NewLayoutId, dto.NewRevision, dto.MappedPlacements, dto.UnmappedPlacements, dto.UnmappedDetails.Select(u => new UnmappedPlacementDetailResponse(u.SourceSkuId, u.SkuCode, u.Barcode, u.Reason)).ToList());

    public static LayoutPointResponse ToResponse(this LayoutPointDto dto) =>
        new(dto.X, dto.Y);

    public static PickingRouteStopResponse ToResponse(this PickingRouteStopDto dto) =>
        new(dto.StopIndex, dto.SkuId, dto.SkuCode, dto.ProductName, dto.Quantity, dto.ShelfId, dto.ShelfCode, dto.LevelNumber, dto.PickCellX, dto.PickCellY);

    public static PickingRoutePreviewResponse ToResponse(this PickingRoutePreviewDto dto) =>
        new(dto.LayoutId, dto.LayoutVersion, dto.StartCell.ToResponse(), dto.OrderedStops.Select(s => s.ToResponse()).ToList(), dto.PathCells.Select(p => p.ToResponse()).ToList(), dto.MissingSkuIds, dto.UnreachableSkuIds, dto.RouteNotice);
}
