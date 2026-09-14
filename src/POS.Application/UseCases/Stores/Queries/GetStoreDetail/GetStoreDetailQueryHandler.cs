using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;
using POS.Application.Abstractions.Persistence;
using POS.Application.Common;
using POS.Domain.Common;

namespace POS.Application.UseCases.Stores.Queries.GetStoreDetail;

public class GetStoreDetailQueryHandler(IStoreRepository storeRepository, IEmployeeRepository employees,
    IEmployeeStoreAccessRepository access, ICurrentUser currentUser) : IQueryHandler<GetStoreDetailQuery, StoreDetailDto>
{
  public async Task<Result<StoreDetailDto>> Handle(GetStoreDetailQuery query, CancellationToken cancellationToken)
  {
    var caller = await StoreManagementAccess.GetOwnerAsync(currentUser, employees, cancellationToken);
    if (caller.IsFailure) return caller.Error;
    if (!await StoreManagementAccess.CanAccessAsync(caller.Value!, query.StoreId, access, cancellationToken))
      return StoreManagementAccess.Forbidden;

    var store = await storeRepository.GetByIdAsync(query.StoreId, cancellationToken);

    if (store is null)
      return CommonErrors.NotFound("Store");

    return StoreDetailDto.FromStore(store);

  }
}
