namespace POS.Contracts.V1.StoreLayouts;

public sealed record ProductPlacementResponse(
    Guid Id,
    Guid ShelfLevelId,
    Guid SkuId,
    bool IsPrimary,
    string? Note,
    Guid UpdatedBy,
    DateTime UpdatedAt,
    int LayoutRevision);

public sealed record ProductLocationItemResponse(
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

public sealed record ProductLocationsResponse(
    Guid SkuId,
    Guid? LayoutId,
    int? LayoutVersion,
    bool HasPlacement,
    IReadOnlyList<ProductLocationItemResponse> Locations);

public sealed record ShelfSkuContentResponse(
    Guid PlacementId,
    Guid SkuId,
    string SkuCode,
    string ProductName,
    string? Barcode,
    bool IsPrimary,
    string? Note);

public sealed record ShelfLevelContentsResponse(
    Guid LevelId,
    int LevelNumber,
    string? Label,
    IReadOnlyList<ShelfSkuContentResponse> SkuPlacements);

public sealed record ShelfContentsResponse(
    Guid ShelfId,
    string ShelfCode,
    string ShelfName,
    IReadOnlyList<ShelfLevelContentsResponse> Levels);

public sealed record PlacementHistoryResponse(
    Guid Id,
    Guid SkuId,
    string Action,
    Guid? FromShelfLevelId,
    Guid? ToShelfLevelId,
    Guid PerformedBy,
    string? Note,
    DateTime CreatedAt);
