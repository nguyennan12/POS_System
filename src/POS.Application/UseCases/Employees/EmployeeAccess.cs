using POS.Domain.Employees;
using POS.Domain.Rbac;
using POS.Domain.Rbac.Constants;

namespace POS.Application.UseCases.Employees;

// Business hierarchy only. Authentication and permission codes belong to AuthorizationBehavior.
public static class EmployeeAccess
{
    public static int Level(Role role, bool isChainOwner) => isChainOwner ? 4 :
        role.StoreId != null ? 0 : role.Name switch
        {
            RoleNames.Owner => 3,
            RoleNames.StoreManager => 2,
            RoleNames.Cashier => 1,
            _ => 0
        };

    public static int Level(Employee employee) => Level(employee.Role, employee.IsChainOwner);
    public static bool CanRead(Employee caller, Employee target) =>
        Level(caller) <= 1 ? caller.Id == target.Id :
        caller.IsChainOwner || (caller.StoreId != null && caller.StoreId == target.StoreId);

    public static bool CanManage(Employee caller, Employee target) =>
        caller.Id != target.Id &&
        (caller.IsChainOwner || (Level(caller) > Level(target) && caller.StoreId != null && caller.StoreId == target.StoreId));

    public static bool CanAssign(Employee caller, Role role, Guid? storeId, bool isChainOwner) =>
        (caller.IsChainOwner || (caller.StoreId != null && caller.StoreId == storeId)) &&
        ((caller.IsChainOwner && isChainOwner) || Level(role, isChainOwner) < Level(caller));

    public static bool CanTransfer(Employee target) =>
        !target.IsChainOwner && target.Role.StoreId == null &&
        target.Role.Name is RoleNames.Cashier or RoleNames.StoreManager;
}
