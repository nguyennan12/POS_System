using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;
using POS.Application.Abstractions.Persistence;
using POS.Application.Common;
using POS.Application.UseCases.Stores.Queries.GetStoreDetail;
using POS.Domain.Common;

namespace POS.Application.UseCases.Stores.Commands.UpdateStoreStatus;

public class UpdateStoreStatusCommandHandler(IStoreRepository stores, IEmployeeRepository employees,
    IEmployeeStoreAccessRepository access, ICurrentUser currentUser, IUnitOfWork unitOfWork)
    : ICommandHandler<UpdateStoreStatusCommand, StoreDetailDto>
{
    public async Task<Result<StoreDetailDto>> Handle(UpdateStoreStatusCommand command, CancellationToken cancellationToken)
    {
        var caller = await StoreManagementAccess.GetOwnerAsync(currentUser, employees, cancellationToken);
        if (caller.IsFailure) return caller.Error;
        if (!await StoreManagementAccess.CanAccessAsync(caller.Value!, command.StoreId, access, cancellationToken))
            return StoreManagementAccess.Forbidden;

        var store = await stores.GetByIdAsync(command.StoreId, cancellationToken);
        if (store is null) return CommonErrors.NotFound("Store");

        // Both directions are supported; repeating the current status is a no-op.
        // Do not require an active store here: Owners must be able to reactivate it.
        store.UpdateStatus(command.IsActive);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return StoreDetailDto.FromStore(store);
    }
}
