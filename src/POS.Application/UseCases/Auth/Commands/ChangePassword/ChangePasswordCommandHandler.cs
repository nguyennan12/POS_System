using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;
using POS.Application.Abstractions.Persistence;
using POS.Application.UseCases.Auth.Errors;
using POS.Domain.Auditing;
using POS.Domain.Common;

namespace POS.Application.UseCases.Auth.Commands.ChangePassword;

public sealed class ChangePasswordCommandHandler(
    ICurrentUser currentUser,
    IEmployeeRepository employeeRepository,
    IPasswordHasher passwordHasher,
    IRefreshTokenRepository refreshTokenRepository,
    IAuditLogRepository auditLogs,
    IUnitOfWork unitOfWork)
    : ICommandHandler<ChangePasswordCommand>
{
    public async Task<Result> Handle(ChangePasswordCommand command, CancellationToken cancellationToken)
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

        if (!passwordHasher.Verify(command.OldPassword, employee.PasswordHash))
        {
            return AuthErrors.InvalidCurrentPassword;
        }

        if (passwordHasher.Verify(command.NewPassword, employee.PasswordHash))
        {
            return AuthErrors.NewPasswordMustBeDifferent;
        }

        employee.ResetCredentialPassword(passwordHasher.Hash(command.NewPassword));
        await refreshTokenRepository.RevokeAllByEmployeeIdAsync(employee.Id, now, cancellationToken);
        await auditLogs.AddAsync(AuditLog.Authentication(employee, AuthenticationAuditActions.PasswordChanged), cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
