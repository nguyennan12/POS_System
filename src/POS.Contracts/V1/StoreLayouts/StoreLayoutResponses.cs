namespace POS.Contracts.V1.StoreLayouts;

public sealed record StoreLayoutSummaryResponse(
    Guid Id,
    Guid StoreId,
    int VersionNumber,
    string Name,
    int GridWidth,
    int GridHeight,
    string Status,
    int Revision,
    DateTime CreatedAt,
    DateTime? PublishedAt);

public sealed record StoreZoneResponse(
    Guid Id,
    Guid LayoutId,
    string Code,
    string Name,
    string Color,
    int PositionX,
    int PositionY,
    int Width,
    int Height);

public sealed record LayoutCellResponse(
    Guid Id,
    int X,
    int Y,
    string CellType,
    int TraversalCost);

public sealed record ShelfLevelResponse(
    Guid Id,
    Guid ShelfId,
    int LevelNumber,
    string? Label,
    int SortOrder,
    IReadOnlyList<ProductPlacementResponse>? Placements);

public sealed record ShelfResponse(
    Guid Id,
    Guid LayoutId,
    Guid? ZoneId,
    string Code,
    string Name,
    int PositionX,
    int PositionY,
    int Width,
    int Height,
    string Orientation,
    string Status,
    IReadOnlyList<ShelfLevelResponse> Levels);

public sealed record StoreLayoutDetailResponse(
    Guid Id,
    Guid StoreId,
    int VersionNumber,
    string Name,
    int GridWidth,
    int GridHeight,
    string Status,
    int Revision,
    Guid CreatedBy,
    DateTime CreatedAt,
    Guid? PublishedBy,
    DateTime? PublishedAt,
    IReadOnlyList<StoreZoneResponse> Zones,
    IReadOnlyList<LayoutCellResponse> Cells,
    IReadOnlyList<ShelfResponse> Shelves);

public sealed record CloneStoreLayoutResponse(
    Guid NewLayoutId,
    int NewRevision,
    int MappedPlacements,
    int UnmappedPlacements,
    IReadOnlyList<UnmappedPlacementDetailResponse> UnmappedDetails);

public sealed record UnmappedPlacementDetailResponse(
    Guid SourceSkuId,
    string SkuCode,
    string? Barcode,
    string Reason);

public sealed record LayoutMutationResponse(
    Guid LayoutId,
    int NewRevision);
