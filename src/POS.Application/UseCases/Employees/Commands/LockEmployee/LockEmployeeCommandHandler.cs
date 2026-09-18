using Microsoft.Extensions.Logging;
using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Caching;
using POS.Application.Abstractions.Messaging;
using POS.Application.Abstractions.Persistence;
using POS.Domain.Common;

namespace POS.Application.UseCases.Employees.Commands.LockEmployee;

public sealed class LockEmployeeCommandHandler(IEmployeeRepository employees, IStoreRepository stores,
    IAuditLogRepository audits, IUnitOfWork unitOfWork, ICurrentUser currentUser,
    ICacheService cache, ILogger<LockEmployeeCommandHandler> logger)
    : ICommandHandler<LockEmployeeCommand, EmployeeDto>
{
    public async Task<Result<EmployeeDto>> Handle(LockEmployeeCommand command, CancellationToken cancellationToken)
    {
        var activeChanged = false;
        var result = await EmployeeTransactions.ExecuteAsync<EmployeeDto>(unitOfWork, async ct =>
        {
            var caller = await employees.GetDetailAsync(currentUser.EmployeeId!.Value, ct);
            var target = await employees.GetDetailAsync(command.Id, ct);
            if (target is null) return (Result<EmployeeDto>)EmployeeErrors.NotFound;
            if (caller is null || !EmployeeAccess.CanManage(caller, target)) return EmployeeErrors.Forbidden;
            if (!command.IsActive && target.IsActive && EmployeeAccess.Level(target) >= 3 &&
                await employees.CountActiveOwnersInStoreAsync(target.StoreId, ct) <= 1)
                return EmployeeErrors.LastOwner;
            var storeError = await EmployeeMutationSupport.ValidateStoreAsync(target.StoreId, target.IsChainOwner, stores, ct);
            if (storeError != Error.None) return storeError;
            activeChanged = target.IsActive != command.IsActive;
            target.SetActive(command.IsActive);
            return await EmployeeMutationSupport.SaveAsync(target, caller.Id,
                command.IsActive ? "Employee.Unlocked" : "Employee.Locked", employees, audits, unitOfWork, ct);
        }, false, cancellationToken);
        if (result.IsSuccess && activeChanged)
            await EmployeeMutationSupport.InvalidateAsync(command.Id, cache, logger, cancellationToken);
        return result;
    }
}
