using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;
using POS.Application.Abstractions.Persistence;
using POS.Domain.Common;

namespace POS.Application.UseCases.Employees.Queries.GetEmployeeOptions;

public sealed class GetEmployeeOptionsQueryHandler(IEmployeeRepository employees, IStoreRepository stores,
    IRoleRepository roles, ICurrentUser currentUser) : IQueryHandler<GetEmployeeOptionsQuery, EmployeeOptionsDto>
{
    public async Task<Result<EmployeeOptionsDto>> Handle(GetEmployeeOptionsQuery query, CancellationToken cancellationToken)
    {
        var caller = await employees.GetDetailAsync(currentUser.EmployeeId!.Value, cancellationToken);
        if (caller is null) return EmployeeErrors.Forbidden;
        var accessibleStores = await stores.GetAccessibleAsync(caller.Id, caller.IsChainOwner, caller.StoreId, cancellationToken);
        var availableRoles = await roles.GetRolesAsync(caller.IsChainOwner ? null : caller.StoreId, cancellationToken);
        var options = new List<EmployeeRoleOptionDto>();
        foreach (var role in availableRoles)
        {
            var detail = await roles.GetByIdWithPermissionsAsync(role.Id, cancellationToken);
            options.Add(new(role.Id, role.Name, role.StoreId, EmployeeAccess.Level(role, false), role.IsSystemRole,
                detail!.RolePermissions.Select(p => p.Permission.Code).Order().ToList()));
        }
        return new EmployeeOptionsDto(accessibleStores.OrderBy(s => s.Name)
            .Select(s => new EmployeeStoreOptionDto(s.Id, s.Name, s.IsActive)).ToList(), options,
            [new(true, "Active"), new(false, "Locked")]);
    }
}
