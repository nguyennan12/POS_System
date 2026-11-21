namespace POS.Contracts.V1.StoreLayouts;

public sealed record LayoutPointRequest(int X, int Y);

public sealed record PickingRouteItemRequest(
    Guid SkuId,
    decimal Quantity);

public sealed record PreviewPickingRouteRequest(
    IReadOnlyList<PickingRouteItemRequest> Items,
    Guid? StoreId = null,
    LayoutPointRequest? StartCell = null);

public sealed record LayoutPointResponse(int X, int Y);

public sealed record PickingRouteStopResponse(
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

public sealed record PickingRoutePreviewResponse(
    Guid LayoutId,
    int LayoutVersion,
    LayoutPointResponse StartCell,
    IReadOnlyList<PickingRouteStopResponse> OrderedStops,
    IReadOnlyList<LayoutPointResponse> PathCells,
    IReadOnlyList<Guid> MissingSkuIds,
    IReadOnlyList<Guid> UnreachableSkuIds,
    string RouteNotice);
