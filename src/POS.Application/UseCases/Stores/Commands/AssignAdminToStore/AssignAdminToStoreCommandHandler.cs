using Microsoft.Extensions.Logging;
using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Caching;
using POS.Application.Abstractions.Messaging;
using POS.Application.Abstractions.Persistence;
using POS.Application.UseCases.Stores.Errors;
using POS.Application.UseCases.Stores.Queries.GetStoreDetail;
using POS.Domain.Common;
using POS.Domain.Rbac.Constants;

namespace POS.Application.UseCases.Stores.Commands.AssignAdminToStore;

public class AssignAdminToStoreCommandHandler(IStoreRepository stores, IEmployeeRepository employees,
    IRoleRepository roles, IEmployeeStoreAccessRepository access, ICurrentUser currentUser,
    IUnitOfWork unitOfWork, ICacheService cache, ILogger<AssignAdminToStoreCommandHandler> logger)
    : ICommandHandler<AssignAdminToStoreCommand, StoreDetailDto>
{
    public async Task<Result<StoreDetailDto>> Handle(AssignAdminToStoreCommand command, CancellationToken cancellationToken)
    {
        var result = await unitOfWork.ExecuteSerializableAsync(
            ct => AssignAsync(command, ct), cancellationToken);
        if (result.IsFailure) return result;

        // Invalidate only after the database transaction has committed.
        try
        {
            await cache.RemoveAsync($"perm:{command.EmployeeId}", cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Assignment committed; permission cache invalidation failed for employee {EmployeeId}", command.EmployeeId);
        }
        return result;
    }

    private async Task<Result<StoreDetailDto>> AssignAsync(AssignAdminToStoreCommand command, CancellationToken cancellationToken)
    {
        var caller = await StoreManagementAccess.GetOwnerAsync(currentUser, employees, cancellationToken);
        if (caller.IsFailure) return caller.Error;
        if (!await StoreManagementAccess.CanAccessAsync(caller.Value!, command.StoreId, access, cancellationToken))
            return StoreErrors.Forbidden;

        var store = await stores.GetByIdAsync(command.StoreId, cancellationToken);
        if (store is null) return StoreErrors.StoreNotFound;
        if (!store.IsActive) return StoreErrors.InactiveStore;

        var employee = await employees.GetByIdAsync(command.EmployeeId, cancellationToken);
        if (employee is null) return StoreErrors.EmployeeNotFound;
        var stateError = EmployeeTransferGuards.ValidateState(employee, store);
        if (stateError != Error.None) return stateError;
        if (employee.IsChainOwner ||
            !(StoreManagementAccess.HasSystemRole(employee, RoleNames.Cashier) ||
              StoreManagementAccess.HasSystemRole(employee, RoleNames.StoreManager)))
            return StoreErrors.InvalidEmployeeRole;

        if (employee.StoreId is not Guid sourceStoreId)
            return StoreErrors.InvalidEmployeeStore;
        if (!await StoreManagementAccess.CanAccessAsync(caller.Value!, sourceStoreId, access, cancellationToken))
            return StoreErrors.Forbidden;

        var managerRoles = await roles.GetSystemRolesByNameAsync(RoleNames.StoreManager, cancellationToken);
        if (managerRoles.Count != 1) return StoreErrors.InvalidStoreManagerRole;
        var managerRole = managerRoles[0];

        var transferError = await EmployeeTransferGuards.ValidatePinAndShiftAsync(employee, store.Id, employees, cancellationToken);
        if (transferError != Error.None) return transferError;

        // Existing model: one role and home store per employee; never promote a chain owner
        // or invent a per-store role. The StoreManager role is the actual seeded system role.
        employee.AssignStoreManager(store.Id, managerRole);
        try
        {
            // EF commits the role and store changes together.
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (PersistenceConflictException ex) when (ex.ConstraintName == PersistenceConstraints.EmployeeStorePinLookupUnique)
        {
            return StoreErrors.EmployeePinAlreadyExists;
        }

        return StoreDetailDto.FromStore(store);
    }
}
