using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;
using POS.Application.Abstractions.Persistence;
using POS.Application.Abstractions.StoreLayouts;
using POS.Domain.Common;

namespace POS.Application.UseCases.StoreLayouts.Queries.PreviewPickingRoute;

public class PreviewPickingRouteQueryHandler : IQueryHandler<PreviewPickingRouteQuery, PickingRoutePreviewDto>
{
    private readonly IStoreLayoutRepository _storeLayoutRepository;
    private readonly IProductPlacementRepository _productPlacementRepository;
    private readonly ISkuRepository _skuRepository;
    private readonly IPathFindingService _pathFindingService;
    private readonly IStoreAccessPolicy _storeAccessPolicy;

    public PreviewPickingRouteQueryHandler(
        IStoreLayoutRepository storeLayoutRepository,
        IProductPlacementRepository productPlacementRepository,
        ISkuRepository skuRepository,
        IPathFindingService pathFindingService,
        IStoreAccessPolicy storeAccessPolicy)
    {
        _storeLayoutRepository = storeLayoutRepository;
        _productPlacementRepository = productPlacementRepository;
        _skuRepository = skuRepository;
        _pathFindingService = pathFindingService;
        _storeAccessPolicy = storeAccessPolicy;
    }

    public async Task<Result<PickingRoutePreviewDto>> Handle(PreviewPickingRouteQuery request, CancellationToken cancellationToken)
    {
        var storeResult = _storeAccessPolicy.EnforceStoreAccess(request.StoreId);
        if (storeResult.IsFailure) return Result<PickingRoutePreviewDto>.Failure(storeResult.Error);
        var storeId = storeResult.Value;

        Domain.StoreLayouts.StoreLayout? targetLayout;
        if (request.LayoutId.HasValue)
        {
            targetLayout = await _storeLayoutRepository.GetByIdAsync(request.LayoutId.Value, includeGraph: true, cancellationToken);
            if (targetLayout == null)
            {
                return Result<PickingRoutePreviewDto>.Failure(new Error(ErrorType.NotFound, "LAYOUT_NOT_FOUND", "Không tìm thấy sơ đồ mặt bằng yêu cầu."));
            }
        }
        else
        {
            targetLayout = await _storeLayoutRepository.GetActiveByStoreAsync(storeId, includeGraph: true, cancellationToken);
            if (targetLayout == null)
            {
                return Result<PickingRoutePreviewDto>.Failure(new Error(ErrorType.NotFound, "NO_ACTIVE_LAYOUT", "Cửa hàng chưa có sơ đồ mặt bằng đang hoạt động."));
            }
        }

        var candidates = new List<(Guid SkuId, decimal Quantity, string SkuCode, string ProductName, IReadOnlyList<ProductLocationDto> Locations)>();

        foreach (var item in request.Items)
        {
            var sku = await _skuRepository.GetByIdAsync(item.SkuId, cancellationToken);
            var skuCode = sku?.SkuCode ?? "UNKNOWN";
            var productName = sku?.Product?.Name ?? skuCode;

            var locs = request.LayoutId.HasValue
                ? await _productPlacementRepository.GetLocationsByLayoutAsync(request.LayoutId.Value, item.SkuId, cancellationToken)
                : await _productPlacementRepository.GetActiveLocationsAsync(storeId, item.SkuId, cancellationToken);

            candidates.Add((item.SkuId, item.Quantity, skuCode, productName, locs.Locations));
        }

        var detailDto = StoreLayoutMapper.ToDetailDto(targetLayout);
        var routePreview = _pathFindingService.CalculatePickingRoute(detailDto, request.StartCell, candidates);

        return Result<PickingRoutePreviewDto>.Success(routePreview);
    }
}
