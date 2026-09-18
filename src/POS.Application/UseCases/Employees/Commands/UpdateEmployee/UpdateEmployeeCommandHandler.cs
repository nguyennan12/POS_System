using Microsoft.Extensions.Logging;
using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Caching;
using POS.Application.Abstractions.Messaging;
using POS.Application.Abstractions.Persistence;
using POS.Application.UseCases.Stores;
using POS.Domain.Common;

namespace POS.Application.UseCases.Employees.Commands.UpdateEmployee;

public sealed class UpdateEmployeeCommandHandler(IEmployeeRepository employees, IRoleRepository roles,
    IStoreRepository stores, IEmployeeStoreAccessRepository accesses, IAuditLogRepository audits,
    IUnitOfWork unitOfWork, ICurrentUser currentUser, ICacheService cache, ILogger<UpdateEmployeeCommandHandler> logger)
    : ICommandHandler<UpdateEmployeeCommand, EmployeeDto>
{
    public async Task<Result<EmployeeDto>> Handle(UpdateEmployeeCommand command, CancellationToken cancellationToken)
    {
        var permissionChanged = false;
        var result = await EmployeeTransactions.ExecuteAsync<EmployeeDto>(unitOfWork, async ct =>
        {
            permissionChanged = false;
            var caller = await employees.GetDetailAsync(currentUser.EmployeeId!.Value, ct);
            var target = await employees.GetDetailAsync(command.Id, ct);
            if (target is null) return (Result<EmployeeDto>)EmployeeErrors.NotFound;
            if (caller is null || !EmployeeAccess.CanManage(caller, target)) return EmployeeErrors.Forbidden;
            if (target.IsChainOwner != command.IsChainOwner && !caller.IsChainOwner) return EmployeeErrors.Forbidden;
            if (target.StoreId != command.StoreId && !EmployeeAccess.CanTransfer(target)) return EmployeeErrors.TransferForbidden;
            var role = await roles.GetByIdAsync(command.RoleId, ct);
            if (role is null) return EmployeeErrors.InvalidRole;
            if (!EmployeeAccess.CanAssign(caller, role, command.StoreId, command.IsChainOwner)) return EmployeeErrors.Forbidden;
            if (!EmployeeMutationSupport.RoleMatchesStore(role, command.StoreId)) return EmployeeErrors.InvalidRole;

            var oldStoreError = await EmployeeMutationSupport.ValidateStoreAsync(target.StoreId, target.IsChainOwner, stores, ct);
            if (oldStoreError != Error.None) return oldStoreError;
            var newStoreError = await EmployeeMutationSupport.ValidateStoreAsync(command.StoreId, command.IsChainOwner, stores, ct);
            if (newStoreError != Error.None) return newStoreError;
            if (target.StoreId != command.StoreId)
            {
                // A transferable (non-chain) employee must have a concrete destination.
                if (command.StoreId is not Guid destinationId) return EmployeeErrors.InvalidStore;
                var destination = (await stores.GetByIdAsync(destinationId, ct))!;
                var stateError = EmployeeTransferGuards.ValidateState(target, destination);
                if (stateError != Error.None) return stateError;
                var transferError = await EmployeeTransferGuards.ValidatePinAndShiftAsync(target, destinationId, employees, ct);
                if (transferError != Error.None) return transferError;
            }
            permissionChanged = target.RoleId != command.RoleId || target.StoreId != command.StoreId ||
                target.IsChainOwner != command.IsChainOwner;
            if (target.IsChainOwner && !command.IsChainOwner)
                await accesses.RemoveAllByEmployeeIdAsync(target.Id, ct);
            target.UpdateProfile(command.Name.Trim(), command.RoleId, command.StoreId, command.IsChainOwner);
            return await EmployeeMutationSupport.SaveAsync(target, caller.Id, "Employee.Updated", employees, audits, unitOfWork, ct);
        }, true, cancellationToken);
        if (result.IsSuccess && permissionChanged)
            await EmployeeMutationSupport.InvalidateAsync(command.Id, cache, logger, cancellationToken);
        return result;
    }
}
