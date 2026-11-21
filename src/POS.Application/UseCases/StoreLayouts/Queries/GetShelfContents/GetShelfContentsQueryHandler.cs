using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;
using POS.Application.Abstractions.Persistence;
using POS.Domain.Common;

namespace POS.Application.UseCases.StoreLayouts.Queries.GetShelfContents;

public class GetShelfContentsQueryHandler : IQueryHandler<GetShelfContentsQuery, ShelfContentsDto>
{
    private readonly IProductPlacementRepository _productPlacementRepository;
    private readonly IStoreAccessPolicy _storeAccessPolicy;

    public GetShelfContentsQueryHandler(
        IProductPlacementRepository productPlacementRepository,
        IStoreAccessPolicy storeAccessPolicy)
    {
        _productPlacementRepository = productPlacementRepository;
        _storeAccessPolicy = storeAccessPolicy;
    }

    public async Task<Result<ShelfContentsDto>> Handle(GetShelfContentsQuery request, CancellationToken cancellationToken)
    {
        var storeResult = _storeAccessPolicy.EnforceStoreAccess(request.StoreId);
        if (storeResult.IsFailure) return Result<ShelfContentsDto>.Failure(storeResult.Error);
        var storeId = storeResult.Value;

        var contents = await _productPlacementRepository.GetShelfContentsAsync(storeId, request.ShelfId, cancellationToken);
        if (contents == null)
            return Result<ShelfContentsDto>.Failure(StoreLayoutErrors.ShelfNotFound);

        return Result<ShelfContentsDto>.Success(contents);
    }
}
