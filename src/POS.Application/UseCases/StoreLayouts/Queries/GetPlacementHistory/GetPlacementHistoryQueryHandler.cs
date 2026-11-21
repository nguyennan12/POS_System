using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;
using POS.Application.Abstractions.Persistence;
using POS.Domain.Common;

namespace POS.Application.UseCases.StoreLayouts.Queries.GetPlacementHistory;

public class GetPlacementHistoryQueryHandler : IQueryHandler<GetPlacementHistoryQuery, PagedPlacementHistoryDto>
{
    private readonly IProductPlacementRepository _productPlacementRepository;
    private readonly IStoreAccessPolicy _storeAccessPolicy;

    public GetPlacementHistoryQueryHandler(
        IProductPlacementRepository productPlacementRepository,
        IStoreAccessPolicy storeAccessPolicy)
    {
        _productPlacementRepository = productPlacementRepository;
        _storeAccessPolicy = storeAccessPolicy;
    }

    public async Task<Result<PagedPlacementHistoryDto>> Handle(GetPlacementHistoryQuery request, CancellationToken cancellationToken)
    {
        var storeResult = _storeAccessPolicy.EnforceStoreAccess(request.StoreId);
        if (storeResult.IsFailure) return Result<PagedPlacementHistoryDto>.Failure(storeResult.Error);
        var storeId = storeResult.Value;

        var page = request.PageNumber > 0 ? request.PageNumber : 1;
        var size = request.PageSize > 0 ? request.PageSize : 20;

        var (items, total) = await _productPlacementRepository.GetHistoryAsync(storeId, request.SkuId, page, size, cancellationToken);
        return Result<PagedPlacementHistoryDto>.Success(new PagedPlacementHistoryDto(items, total, page, size));
    }
}
