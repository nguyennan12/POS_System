using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;
using POS.Application.Abstractions.Persistence;
using POS.Domain.Common;

namespace POS.Application.UseCases.Stores.Queries.GetAllStores;

public class GetAllStoresQueryHandler(IStoreRepository stores, IEmployeeRepository employees, ICurrentUser currentUser)
    : IQueryHandler<GetAllStoresQuery, List<StoreDto>>
{
    public async Task<Result<List<StoreDto>>> Handle(GetAllStoresQuery query, CancellationToken cancellationToken)
    {
        var caller = await StoreManagementAccess.GetOwnerAsync(currentUser, employees, cancellationToken);
        if (caller.IsFailure) return caller.Error;
        var owner = caller.Value!;
        var visibleStores = await stores.GetAccessibleAsync(owner.Id, owner.IsChainOwner, owner.StoreId, cancellationToken);
        return visibleStores.Select(s => new StoreDto(s.Id, s.Name, s.Address, s.Phone, s.IsActive)).ToList();
    }
}
