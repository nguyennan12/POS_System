using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Persistence;
using POS.Domain.Rbac;

namespace POS.Application.UseCases.Rbac;

internal static class RoleAccessControl
{
    public static async Task<bool> CanManageStoreAsync(
        ICurrentUser currentUser,
        Guid? targetStoreId,
        IEmployeeStoreAccessRepository accessRepo,
        CancellationToken cancellationToken)
    {
        if (currentUser.IsChainOwner) return true;
        if (!targetStoreId.HasValue) return false; // Non-chain owner cannot manage chain-wide or null-store roles

        if (currentUser.StoreId.HasValue && currentUser.StoreId.Value == targetStoreId.Value)
            return true;

        if (currentUser.EmployeeId.HasValue)
        {
            return await accessRepo.ExistsAsync(currentUser.EmployeeId.Value, targetStoreId.Value, cancellationToken);
        }

        return false;
    }

    public static async Task<bool> CanReadRoleAsync(
        ICurrentUser currentUser,
        Role role,
        IEmployeeStoreAccessRepository accessRepo,
        CancellationToken cancellationToken)
    {
        if (role.IsSystemRole || role.StoreId == null) return true;
        if (currentUser.IsChainOwner) return true;

        if (currentUser.StoreId.HasValue && currentUser.StoreId.Value == role.StoreId.Value)
            return true;

        if (currentUser.EmployeeId.HasValue && role.StoreId.HasValue)
        {
            return await accessRepo.ExistsAsync(currentUser.EmployeeId.Value, role.StoreId.Value, cancellationToken);
        }

        return false;
    }
}
