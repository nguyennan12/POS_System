using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;
using POS.Application.Abstractions.Persistence;
using POS.Domain.Common;

namespace POS.Application.UseCases.StoreLayouts.Queries.GetActiveStoreLayout;

public class GetActiveStoreLayoutQueryHandler : IQueryHandler<GetActiveStoreLayoutQuery, StoreLayoutDetailDto?>
{
    private readonly IStoreLayoutRepository _storeLayoutRepository;
    private readonly IStoreAccessPolicy _storeAccessPolicy;

    public GetActiveStoreLayoutQueryHandler(
        IStoreLayoutRepository storeLayoutRepository,
        IStoreAccessPolicy storeAccessPolicy)
    {
        _storeLayoutRepository = storeLayoutRepository;
        _storeAccessPolicy = storeAccessPolicy;
    }

    public async Task<Result<StoreLayoutDetailDto?>> Handle(GetActiveStoreLayoutQuery request, CancellationToken cancellationToken)
    {
        var storeResult = _storeAccessPolicy.EnforceStoreAccess(request.StoreId);
        if (storeResult.IsFailure) return Result<StoreLayoutDetailDto?>.Failure(storeResult.Error);
        var storeId = storeResult.Value;

        var layout = await _storeLayoutRepository.GetActiveByStoreAsync(storeId, includeGraph: true, cancellationToken);
        if (layout == null)
            return Result<StoreLayoutDetailDto?>.Success(null);

        return Result<StoreLayoutDetailDto?>.Success(StoreLayoutMapper.ToDetailDto(layout));
    }
}
