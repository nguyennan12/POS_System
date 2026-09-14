using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;
using POS.Application.Abstractions.Persistence;
using POS.Application.Common;
using POS.Application.UseCases.Stores.Queries.GetStoreDetail;
using POS.Domain.Common;
using POS.Domain.Employees;
using POS.Domain.Rbac.Constants;

namespace POS.Application.UseCases.Stores.Commands.GrantOwnerAccess;

public class GrantOwnerAccessCommandHandler(IStoreRepository stores, IEmployeeRepository employees,
    IEmployeeStoreAccessRepository access, ICurrentUser currentUser, IUnitOfWork unitOfWork)
    : ICommandHandler<GrantOwnerAccessCommand, StoreDetailDto>
{
    public async Task<Result<StoreDetailDto>> Handle(GrantOwnerAccessCommand command, CancellationToken cancellationToken)
    {
        var caller = await StoreManagementAccess.GetOwnerAsync(currentUser, employees, cancellationToken);
        if (caller.IsFailure) return caller.Error;
        if (!await StoreManagementAccess.CanAccessAsync(caller.Value!, command.StoreId, access, cancellationToken))
            return StoreManagementAccess.Forbidden;

        var store = await stores.GetByIdAsync(command.StoreId, cancellationToken);
        if (store is null) return CommonErrors.NotFound("Store");
        if (!store.IsActive) return CommonErrors.Invalid("StoreStatus");

        var recipient = await employees.GetByIdAsync(command.EmployeeId, cancellationToken);
        if (recipient is null) return CommonErrors.NotFound("Employee");
        if (!recipient.IsActive || recipient.LockedUntil > DateTime.UtcNow)
            return CommonErrors.Invalid("EmployeeStatus");
        if (!recipient.IsChainOwner || !StoreManagementAccess.HasSystemRole(recipient, RoleNames.Owner))
            return CommonErrors.Invalid("OwnerAccessRecipient");

        if (await access.ExistsAsync(recipient.Id, store.Id, cancellationToken))
            return CommonErrors.AlreadyExists("EmployeeStoreAccess");

        // Grant only this store. Do not change role, IsChainOwner or the recipient's home store.
        await access.AddAsync(new EmployeeStoreAccess(recipient.Id, store.Id, caller.Value!.Id), cancellationToken);
        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DuplicateStoreAccessException)
        {
            // A concurrent grant may have passed the existence check too.
            return CommonErrors.AlreadyExists("EmployeeStoreAccess");
        }

        // Store scope is read directly from the DB; it is not cached by this feature.
        return StoreDetailDto.FromStore(store);
    }
}
