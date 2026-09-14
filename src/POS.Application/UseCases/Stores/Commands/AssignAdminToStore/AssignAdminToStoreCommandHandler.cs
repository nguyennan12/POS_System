using Microsoft.Extensions.Logging;
using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Caching;
using POS.Application.Abstractions.Messaging;
using POS.Application.Abstractions.Persistence;
using POS.Application.Common;
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
            return StoreManagementAccess.Forbidden;

        var store = await stores.GetByIdAsync(command.StoreId, cancellationToken);
        if (store is null) return CommonErrors.NotFound("Store");
        if (!store.IsActive) return CommonErrors.Invalid("StoreStatus");

        var employee = await employees.GetByIdAsync(command.EmployeeId, cancellationToken);
        if (employee is null) return CommonErrors.NotFound("Employee");
        if (!employee.IsActive || employee.LockedUntil > DateTime.UtcNow)
            return CommonErrors.Invalid("EmployeeStatus");
        if (employee.IsChainOwner ||
            !(StoreManagementAccess.HasSystemRole(employee, RoleNames.Cashier) ||
              StoreManagementAccess.HasSystemRole(employee, RoleNames.StoreManager)))
            return CommonErrors.Invalid("EmployeeRole");

        if (employee.StoreId is not Guid sourceStoreId)
            return CommonErrors.Invalid("EmployeeStore");
        if (!await StoreManagementAccess.CanAccessAsync(caller.Value!, sourceStoreId, access, cancellationToken))
            return StoreManagementAccess.Forbidden;

        var managerRoles = await roles.GetSystemRolesByNameAsync(RoleNames.StoreManager, cancellationToken);
        if (managerRoles.Count != 1) return CommonErrors.Invalid("StoreManagerRole");
        var managerRole = managerRoles[0];

        if (employee.StoreId != store.Id)
        {
            if (await employees.HasOpenShiftOutsideStoreAsync(employee.Id, store.Id, cancellationToken))
                return CommonErrors.Invalid("EmployeeOpenShift");

            // T12/T20 own HMAC lookup generation/reset. A BCrypt hash cannot be used
            // to reconstruct it. Refuse a transfer whose PIN uniqueness is unknowable.
            if (string.IsNullOrWhiteSpace(employee.PinLookupHash) ||
                await employees.HasMissingPinLookupAsync(store.Id, cancellationToken))
                return new Error(ErrorType.Invalid, "Employee.PinLookupMissing",
                    "Chưa đủ dữ liệu PIN lookup để kiểm tra trùng PIN. Cần hoàn thiện/reset PIN qua T12/T20 trước khi chuyển cửa hàng.");

            if (await employees.HasPinConflictAsync(employee.Id, store.Id, employee.PinLookupHash, cancellationToken))
                return CommonErrors.AlreadyExists("EmployeePin");
        }

        // Existing model: one role and home store per employee; never promote a chain owner
        // or invent a per-store role. The StoreManager role is the actual seeded system role.
        employee.AssignStoreManager(store.Id, managerRole);
        try
        {
            // EF commits the role and store changes together.
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (EmployeeAssignmentConflictException)
        {
            return CommonErrors.AlreadyExists("EmployeePin");
        }

        return StoreDetailDto.FromStore(store);
    }
}
