using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;
using POS.Application.Abstractions.Persistence;
using POS.Domain.Common;

namespace POS.Application.UseCases.StoreLayouts.Queries.GetStoreLayoutById;

public class GetStoreLayoutByIdQueryHandler : IQueryHandler<GetStoreLayoutByIdQuery, StoreLayoutDetailDto>
{
    private readonly IStoreLayoutRepository _storeLayoutRepository;
    private readonly IStoreAccessPolicy _storeAccessPolicy;

    public GetStoreLayoutByIdQueryHandler(
        IStoreLayoutRepository storeLayoutRepository,
        IStoreAccessPolicy storeAccessPolicy)
    {
        _storeLayoutRepository = storeLayoutRepository;
        _storeAccessPolicy = storeAccessPolicy;
    }

    public async Task<Result<StoreLayoutDetailDto>> Handle(GetStoreLayoutByIdQuery request, CancellationToken cancellationToken)
    {
        var layout = await _storeLayoutRepository.GetByIdAsync(request.LayoutId, includeGraph: true, cancellationToken);
        if (layout == null)
            return Result<StoreLayoutDetailDto>.Failure(StoreLayoutErrors.NotFound);

        if (!_storeAccessPolicy.CanAccessStore(layout.StoreId))
            return Result<StoreLayoutDetailDto>.Failure(StoreLayoutErrors.StoreMismatch);

        return Result<StoreLayoutDetailDto>.Success(StoreLayoutMapper.ToDetailDto(layout));
    }
}
