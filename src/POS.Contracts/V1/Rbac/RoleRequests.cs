namespace POS.Contracts.V1.Rbac;

public record CreateRoleRequest(
    string Name,
    string? Description = null,
    Guid? StoreId = null
);

public record UpdateRoleRequest(
    string Name,
    string? Description = null
);

public record UpdateRolePermissionsRequest(
    IReadOnlyList<Guid> PermissionIds
);
