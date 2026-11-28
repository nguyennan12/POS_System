using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;
using POS.Application.Abstractions.Persistence;
using POS.Domain.Common;

namespace POS.Application.UseCases.StoreLayouts.Queries.GetProductLocations;

public class GetProductLocationsQueryHandler : IQueryHandler<GetProductLocationsQuery, ProductLocationsDto>
{
    private readonly IProductPlacementRepository _productPlacementRepository;
    private readonly ISkuRepository _skuRepository;
    private readonly IStoreAccessPolicy _storeAccessPolicy;

    public GetProductLocationsQueryHandler(
        IProductPlacementRepository productPlacementRepository,
        ISkuRepository skuRepository,
        IStoreAccessPolicy storeAccessPolicy)
    {
        _productPlacementRepository = productPlacementRepository;
        _skuRepository = skuRepository;
        _storeAccessPolicy = storeAccessPolicy;
    }

    public async Task<Result<ProductLocationsDto>> Handle(GetProductLocationsQuery request, CancellationToken cancellationToken)
    {
        var storeResult = _storeAccessPolicy.EnforceStoreAccess(request.StoreId);
        if (storeResult.IsFailure) return Result<ProductLocationsDto>.Failure(storeResult.Error);
        var storeId = storeResult.Value;

        var sku = await _skuRepository.GetByIdAsync(request.SkuId, cancellationToken);
        if (sku == null || sku.StoreId != storeId)
        {
            return Result<ProductLocationsDto>.Failure(StoreLayoutErrors.SkuNotInStore);
        }

        var locations = await _productPlacementRepository.GetActiveLocationsAsync(storeId, request.SkuId, cancellationToken);
        return Result<ProductLocationsDto>.Success(locations);
    }
}
