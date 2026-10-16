using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;
using POS.Application.Abstractions.Persistence;
using POS.Application.UseCases.Stores.Errors;
using POS.Domain.Common;

namespace POS.Application.UseCases.Stores.Queries.GetStoreDetail;

public class GetStoreDetailQueryHandler(
    IStoreRepository storeRepository,
    IStoreAccessPolicy storeAccessPolicy) : IQueryHandler<GetStoreDetailQuery, StoreDetailDto>
{
  public async Task<Result<StoreDetailDto>> Handle(GetStoreDetailQuery query, CancellationToken cancellationToken)
  {
    var caller = await storeAccessPolicy.GetAuthenticatedOwnerAsync(cancellationToken);
    if (caller.IsFailure) return caller.Error;
    if (!storeAccessPolicy.CanAccessStore(query.StoreId))
      return StoreErrors.Forbidden;

    var store = await storeRepository.GetByIdAsync(query.StoreId, cancellationToken);

    if (store is null)
      return StoreErrors.StoreNotFound;

    return StoreDetailDto.FromStore(store);
  }
}
