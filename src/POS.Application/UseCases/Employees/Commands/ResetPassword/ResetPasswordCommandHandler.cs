using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;
using POS.Application.Abstractions.Persistence;
using POS.Domain.Common;

namespace POS.Application.UseCases.Employees.Commands.ResetPassword;

public sealed class ResetPasswordCommandHandler(IEmployeeRepository employees, IStoreRepository stores,
    IAuditLogRepository audits, IRefreshTokenRepository refreshTokens, IUnitOfWork unitOfWork,
    ICurrentUser currentUser, IPasswordHasher passwordHasher)
    : ICommandHandler<ResetPasswordCommand, EmployeeDto>
{
    public Task<Result<EmployeeDto>> Handle(ResetPasswordCommand command, CancellationToken cancellationToken) =>
        EmployeeTransactions.ExecuteAsync<EmployeeDto>(unitOfWork, async ct =>
        {
            var caller = await employees.GetDetailAsync(currentUser.EmployeeId!.Value, ct);
            var target = await employees.GetDetailAsync(command.Id, ct);
            if (target is null) return (Result<EmployeeDto>)EmployeeErrors.NotFound;
            if (caller is null || !EmployeeAccess.CanManage(caller, target)) return EmployeeErrors.Forbidden;
            var storeError = await EmployeeMutationSupport.ValidateStoreAsync(target.StoreId, target.IsChainOwner, stores, ct);
            if (storeError != Error.None) return storeError;
            target.ResetCredentialPassword(passwordHasher.Hash(command.NewPassword));
            await refreshTokens.RevokeAllByEmployeeIdAsync(target.Id, DateTime.UtcNow, ct);
            return await EmployeeMutationSupport.SaveAsync(target, caller.Id, "Employee.PasswordReset",
                employees, audits, unitOfWork, ct);
        }, false, cancellationToken);
}
