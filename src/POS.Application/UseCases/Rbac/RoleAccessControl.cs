using POS.Application.Abstractions.Auth;
using POS.Domain.Rbac;

namespace POS.Application.UseCases.Rbac;

internal static class RoleAccessControl
{
    public static bool CanManageStore(
        ICurrentUser currentUser,
        Guid? targetStoreId)
    {
        if (currentUser.IsChainOwner) return true;
        if (!targetStoreId.HasValue) return false;

        return currentUser.StoreId.HasValue && currentUser.StoreId.Value == targetStoreId.Value;
    }

    public static bool CanReadRole(
        ICurrentUser currentUser,
        Role role)
    {
        if (role.IsSystemRole || role.StoreId == null) return true;
        if (currentUser.IsChainOwner) return true;

        return currentUser.StoreId.HasValue && currentUser.StoreId.Value == role.StoreId.Value;
    }
}
