using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;
using POS.Application.Abstractions.Persistence;
using POS.Application.UseCases.Auth.Errors;
using POS.Application.UseCases.Employees;
using POS.Domain.Auditing;
using POS.Domain.Common;

namespace POS.Application.UseCases.Auth.Commands.ChangePin;

public sealed class ChangePinCommandHandler(
    ICurrentUser currentUser,
    IEmployeeRepository employeeRepository,
    IPasswordHasher passwordHasher,
    IPinLookupHasher pinLookupHasher,
    IRefreshTokenRepository refreshTokenRepository,
    IAuditLogRepository auditLogs,
    IUnitOfWork unitOfWork)
    : ICommandHandler<ChangePinCommand>
{
    public async Task<Result> Handle(ChangePinCommand command, CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || currentUser.EmployeeId is not Guid employeeId)
        {
            return AuthErrors.Unauthorized;
        }

        var employee = await employeeRepository.GetDetailAsync(employeeId, cancellationToken);
        var now = DateTime.UtcNow;

        if (employee is null || !employee.IsActive || employee.IsLocked(now))
        {
            return employee?.IsLocked(now) == true && employee.IsActive
                ? AuthErrors.AccountLocked
                : AuthErrors.Unauthorized;
        }

        if (!passwordHasher.Verify(command.OldPin, employee.PinHash))
        {
            return AuthErrors.InvalidCurrentPin;
        }

        if (passwordHasher.Verify(command.NewPin, employee.PinHash))
        {
            return AuthErrors.NewPinMustBeDifferent;
        }

        var newLookupHash = pinLookupHasher.ComputeHash(command.NewPin);
        if (await employeeRepository.HasPinConflictAsync(employee.Id, employee.StoreId, newLookupHash, cancellationToken))
        {
            return EmployeeErrors.PinExists;
        }

        employee.ResetCredentialPin(passwordHasher.Hash(command.NewPin), newLookupHash);
        await refreshTokenRepository.RevokeAllByEmployeeIdAsync(employee.Id, now, cancellationToken);
        await auditLogs.AddAsync(AuditLog.Authentication(employee, AuthenticationAuditActions.PinChanged), cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
