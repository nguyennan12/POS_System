using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;
using POS.Application.Abstractions.Persistence;
using POS.Domain.Common;
using POS.Domain.Employees;

namespace POS.Application.UseCases.Employees.Commands.CreateEmployee;

public sealed class CreateEmployeeCommandHandler(IEmployeeRepository employees, IRoleRepository roles,
    IStoreRepository stores, IAuditLogRepository audits, IUnitOfWork unitOfWork, ICurrentUser currentUser,
    IPasswordHasher passwordHasher, IPinLookupHasher pinLookupHasher)
    : ICommandHandler<CreateEmployeeCommand, EmployeeDto>
{
    public Task<Result<EmployeeDto>> Handle(CreateEmployeeCommand command, CancellationToken cancellationToken) =>
        EmployeeTransactions.ExecuteAsync<EmployeeDto>(unitOfWork, ct => CreateAsync(command, ct), true, cancellationToken);

    private async Task<Result<EmployeeDto>> CreateAsync(CreateEmployeeCommand command, CancellationToken cancellationToken)
    {
        var caller = await employees.GetDetailAsync(currentUser.EmployeeId!.Value, cancellationToken);
        if (caller is null || EmployeeAccess.Level(caller) < 2) return EmployeeErrors.Forbidden;
        var role = await roles.GetByIdAsync(command.RoleId, cancellationToken);
        if (role is null) return EmployeeErrors.InvalidRole;
        if (!EmployeeAccess.CanAssign(caller, role, command.StoreId, command.IsChainOwner) ||
            (EmployeeAccess.Level(caller) == 2 && EmployeeAccess.Level(role, command.IsChainOwner) != 1))
            return EmployeeErrors.Forbidden;
        if (!EmployeeMutationSupport.RoleMatchesStore(role, command.StoreId)) return EmployeeErrors.InvalidRole;
        var storeError = await EmployeeMutationSupport.ValidateStoreAsync(command.StoreId, command.IsChainOwner, stores, cancellationToken);
        if (storeError != Error.None) return storeError;

        var username = command.Username.Trim();
        if (await employees.ExistsByUsernameAsync(username, cancellationToken)) return EmployeeErrors.UsernameExists;
        var lookup = pinLookupHasher.ComputeHash(command.Pin);
        if (command.StoreId is Guid storeId &&
            await employees.HasPinConflictAsync(Guid.Empty, storeId, lookup, cancellationToken))
            return EmployeeErrors.PinExists;
        var employee = new Employee(command.Name.Trim(), username, passwordHasher.Hash(command.Password),
            passwordHasher.Hash(command.Pin), role.Id, command.IsChainOwner, command.StoreId);
        employee.setPinLookUpHash(lookup);
        await employees.AddAsync(employee, cancellationToken);
        return await EmployeeMutationSupport.SaveAsync(employee, caller.Id, "Employee.Created",
            employees, audits, unitOfWork, cancellationToken);
    }
}
