using POS.Domain.StoreLayouts.Enums;

namespace POS.Application.UseCases.StoreLayouts;

public sealed record StoreLayoutSummaryDto(
    Guid Id,
    Guid StoreId,
    int VersionNumber,
    string Name,
    int GridWidth,
    int GridHeight,
    StoreLayoutStatus Status,
    int Revision,
    DateTime CreatedAt,
    DateTime? PublishedAt);

public sealed record StoreZoneDto(
    Guid Id,
    Guid LayoutId,
    string Code,
    string Name,
    string Color,
    int PositionX,
    int PositionY,
    int Width,
    int Height);

public sealed record StoreLayoutCellDto(
    Guid Id,
    int X,
    int Y,
    LayoutCellType CellType,
    int TraversalCost);

public sealed record ShelfLevelDto(
    Guid Id,
    Guid ShelfId,
    int LevelNumber,
    string? Label,
    int SortOrder,
    IReadOnlyList<ProductPlacementDto>? Placements);

public sealed record ShelfDto(
    Guid Id,
    Guid LayoutId,
    Guid? ZoneId,
    string Code,
    string Name,
    int PositionX,
    int PositionY,
    int Width,
    int Height,
    ShelfOrientation Orientation,
    ShelfStatus Status,
    IReadOnlyList<ShelfLevelDto> Levels);

public sealed record StoreLayoutDetailDto(
    Guid Id,
    Guid StoreId,
    int VersionNumber,
    string Name,
    int GridWidth,
    int GridHeight,
    StoreLayoutStatus Status,
    int Revision,
    Guid CreatedBy,
    DateTime CreatedAt,
    Guid? PublishedBy,
    DateTime? PublishedAt,
    IReadOnlyList<StoreZoneDto> Zones,
    IReadOnlyList<StoreLayoutCellDto> Cells,
    IReadOnlyList<ShelfDto> Shelves);

public sealed record ProductPlacementDto(
    Guid Id,
    Guid ShelfLevelId,
    Guid SkuId,
    bool IsPrimary,
    string? Note,
    Guid UpdatedBy,
    DateTime UpdatedAt,
    int LayoutRevision);

public sealed record ProductLocationDto(
    Guid ZoneId,
    string ZoneName,
    Guid ShelfId,
    string ShelfCode,
    string ShelfName,
    Guid ShelfLevelId,
    int LevelNumber,
    bool IsPrimary,
    int PickCellX,
    int PickCellY);

public sealed record ProductLocationsDto(
    Guid SkuId,
    Guid? LayoutId,
    int? LayoutVersion,
    bool HasPlacement,
    IReadOnlyList<ProductLocationDto> Locations);

public sealed record ShelfSkuContentDto(
    Guid PlacementId,
    Guid SkuId,
    string SkuCode,
    string ProductName,
    string? Barcode,
    bool IsPrimary,
    string? Note);

public sealed record ShelfLevelContentsDto(
    Guid LevelId,
    int LevelNumber,
    string? Label,
    IReadOnlyList<ShelfSkuContentDto> SkuPlacements);

public sealed record ShelfContentsDto(
    Guid ShelfId,
    string ShelfCode,
    string ShelfName,
    IReadOnlyList<ShelfLevelContentsDto> Levels);

public sealed record PlacementHistoryDto(
    Guid Id,
    Guid SkuId,
    PlacementHistoryAction Action,
    Guid? FromShelfLevelId,
    Guid? ToShelfLevelId,
    Guid PerformedBy,
    string? Note,
    DateTime CreatedAt);

public sealed record PagedStoreLayoutsDto(
    IReadOnlyList<StoreLayoutSummaryDto> Items,
    int TotalCount,
    int PageNumber,
    int PageSize);

public sealed record PagedPlacementHistoryDto(
    IReadOnlyList<PlacementHistoryDto> Items,
    int TotalCount,
    int PageNumber,
    int PageSize);

public sealed record UnmappedPlacementDto(
    Guid SourceSkuId,
    string SkuCode,
    string? Barcode,
    string Reason);

public sealed record CloneStoreLayoutResultDto(
    Guid NewLayoutId,
    int NewRevision,
    int MappedPlacements,
    int UnmappedPlacements,
    IReadOnlyList<UnmappedPlacementDto> UnmappedDetails);

public sealed record LayoutPointDto(int X, int Y);

public sealed record PickingRouteItemDto(
    Guid SkuId,
    decimal Quantity);

public sealed record PickingRouteStopDto(
    int StopIndex,
    Guid SkuId,
    string SkuCode,
    string ProductName,
    decimal Quantity,
    Guid ShelfId,
    string ShelfCode,
    int LevelNumber,
    int PickCellX,
    int PickCellY);

public sealed record PickingRoutePreviewDto(
    Guid LayoutId,
    int LayoutVersion,
    LayoutPointDto StartCell,
    IReadOnlyList<PickingRouteStopDto> OrderedStops,
    IReadOnlyList<LayoutPointDto> PathCells,
    IReadOnlyList<Guid> MissingSkuIds,
    IReadOnlyList<Guid> UnreachableSkuIds,
    string RouteNotice);

public sealed record LayoutCellChangeDto(
    int X,
    int Y,
    LayoutCellType CellType,
    int TraversalCost = 1);
