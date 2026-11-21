using POS.Application.UseCases.StoreLayouts;

namespace POS.Application.Abstractions.StoreLayouts;

public interface IPathFindingService
{
    List<LayoutPointDto>? FindShortestPath(
        int gridWidth,
        int gridHeight,
        HashSet<(int X, int Y)> unwalkable,
        LayoutPointDto start,
        LayoutPointDto target);

    PickingRoutePreviewDto CalculatePickingRoute(
        StoreLayoutDetailDto layout,
        LayoutPointDto? requestedStart,
        IReadOnlyList<(Guid SkuId, decimal Quantity, string SkuCode, string ProductName, IReadOnlyList<ProductLocationDto> Locations)> items);
}
