using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Persistence;
using POS.Application.UseCases.Stores.Errors;
using POS.Domain.Common;
using POS.Domain.Employees;
using POS.Domain.Rbac;
using POS.Domain.Rbac.Constants;

namespace POS.Application.UseCases.Auth;

/// <summary>
/// Hiện thực <see cref="IStoreAccessPolicy"/> — gom toàn bộ logic kiểm tra
/// quyền Owner và phạm vi chi nhánh từ <c>StoreManagementAccess</c> và
/// <c>RoleAccessControl</c> vào một service có thể inject và mock.
/// </summary>
public sealed class StoreAccessPolicy(
    ICurrentUser currentUser,
    IEmployeeRepository employeeRepository) : IStoreAccessPolicy
{
    /// <inheritdoc/>
    public async Task<Result<Employee>> GetAuthenticatedOwnerAsync(CancellationToken ct = default)
    {
        if (!currentUser.IsAuthenticated || currentUser.EmployeeId is not Guid id || id == Guid.Empty)
            return StoreErrors.Unauthorized;

        var employee = await employeeRepository.GetByIdAsync(id, ct);
        if (employee is null || !employee.IsActive || employee.LockedUntil > DateTime.UtcNow ||
            !HasSystemRole(employee, RoleNames.Owner))
            return StoreErrors.Forbidden;

        return employee;
    }

    /// <inheritdoc/>
    public Result<Guid> EnforceStoreAccess(Guid? requestedStoreId = null)
    {
        var targetStoreId = requestedStoreId ?? currentUser.StoreId;
        if (!targetStoreId.HasValue)
            return StoreErrors.StoreRequired;

        if (!CanAccessStore(targetStoreId.Value))
            return StoreErrors.StoreAccessDenied;

        return targetStoreId.Value;
    }

    /// <inheritdoc/>
    public bool CanAccessStore(Guid? targetStoreId)
    {
        if (currentUser.IsChainOwner) return true;
        if (!targetStoreId.HasValue) return false;
        return currentUser.StoreId.HasValue && currentUser.StoreId.Value == targetStoreId.Value;
    }

    /// <inheritdoc/>
    public bool CanAccessRole(Role role)
    {
        if (role.IsSystemRole || role.StoreId == null) return true;
        if (currentUser.IsChainOwner) return true;
        return currentUser.StoreId.HasValue && currentUser.StoreId.Value == role.StoreId.Value;
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    private static bool HasSystemRole(Employee employee, string roleName) =>
        employee.Role is { IsSystemRole: true, StoreId: null } role &&
        role.Id == employee.RoleId && role.Name == roleName;
}
