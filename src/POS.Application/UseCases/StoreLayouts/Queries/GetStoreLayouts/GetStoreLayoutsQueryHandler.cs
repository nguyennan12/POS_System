using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;
using POS.Application.Abstractions.Persistence;
using POS.Domain.Common;

namespace POS.Application.UseCases.StoreLayouts.Queries.GetStoreLayouts;

public class GetStoreLayoutsQueryHandler : IQueryHandler<GetStoreLayoutsQuery, PagedStoreLayoutsDto>
{
    private readonly IStoreLayoutRepository _storeLayoutRepository;
    private readonly IStoreAccessPolicy _storeAccessPolicy;

    public GetStoreLayoutsQueryHandler(
        IStoreLayoutRepository storeLayoutRepository,
        IStoreAccessPolicy storeAccessPolicy)
    {
        _storeLayoutRepository = storeLayoutRepository;
        _storeAccessPolicy = storeAccessPolicy;
    }

    public async Task<Result<PagedStoreLayoutsDto>> Handle(GetStoreLayoutsQuery request, CancellationToken cancellationToken)
    {
        var storeResult = _storeAccessPolicy.EnforceStoreAccess(request.StoreId);
        if (storeResult.IsFailure) return Result<PagedStoreLayoutsDto>.Failure(storeResult.Error);
        var storeId = storeResult.Value;

        var page = request.PageNumber > 0 ? request.PageNumber : 1;
        var size = request.PageSize > 0 ? request.PageSize : 20;

        var (items, total) = await _storeLayoutRepository.ListAsync(storeId, request.Status, page, size, cancellationToken);
        return Result<PagedStoreLayoutsDto>.Success(new PagedStoreLayoutsDto(items, total, page, size));
    }
}
