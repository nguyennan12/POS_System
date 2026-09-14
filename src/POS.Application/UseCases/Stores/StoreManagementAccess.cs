using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Persistence;
using POS.Domain.Common;
using POS.Domain.Employees;
using POS.Domain.Rbac.Constants;

namespace POS.Application.UseCases.Stores;

// Store-specific business preconditions from the Stores API (Owner only).
// This does not implement T16's dynamic permission authorization pipeline.
internal static class StoreManagementAccess
{
    internal static readonly Error Forbidden = new(ErrorType.Forbidden, "Store.Forbidden", "Không có quyền quản lý cửa hàng này.");

    internal static async Task<Result<Employee>> GetOwnerAsync(
        ICurrentUser currentUser, IEmployeeRepository employees, CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || currentUser.EmployeeId is not Guid id || id == Guid.Empty)
            return new Error(ErrorType.Unauthorized, "Store.Unauthorized", "Cần đăng nhập với danh tính nhân viên hợp lệ.");

        var employee = await employees.GetByIdAsync(id, cancellationToken);
        if (employee is null || !employee.IsActive || employee.LockedUntil > DateTime.UtcNow ||
            !HasSystemRole(employee, RoleNames.Owner))
            return Forbidden;

        return employee;
    }

    internal static bool HasSystemRole(Employee employee, string roleName) =>
        employee.Role is { IsSystemRole: true, StoreId: null } role &&
        role.Id == employee.RoleId && role.Name == roleName;

    internal static Task<bool> CanAccessAsync(
        Employee owner, Guid storeId, IEmployeeStoreAccessRepository access, CancellationToken cancellationToken) =>
        owner.IsChainOwner
            ? access.ExistsAsync(owner.Id, storeId, cancellationToken)
            : Task.FromResult(owner.StoreId == storeId);
}
