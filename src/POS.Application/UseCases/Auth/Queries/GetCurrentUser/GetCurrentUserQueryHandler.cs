using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;
using POS.Application.Abstractions.Persistence;
using POS.Application.UseCases.Auth.Dtos;
using POS.Application.UseCases.Auth.Errors;
using POS.Domain.Common;

namespace POS.Application.UseCases.Auth.Queries.GetCurrentUser;

public sealed class GetCurrentUserQueryHandler(
    ICurrentUser currentUser,
    IEmployeeRepository employeeRepository,
    IPermissionRepository permissionRepository)
    : IQueryHandler<GetCurrentUserQuery, CurrentUserDto>
{
    public async Task<Result<CurrentUserDto>> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
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

        var permissions = await permissionRepository.GetPermissionCodesAsync(employee.RoleId, cancellationToken);

        return new CurrentUserDto(
            employee.Id,
            employee.Name,
            employee.Username,
            employee.RoleId,
            employee.Role.Name,
            employee.StoreId,
            employee.Store?.Name,
            employee.IsChainOwner,
            permissions);
    }
}
