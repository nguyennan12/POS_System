namespace POS.Contracts.V1.Rbac;

public record PermissionResponse(
    Guid Id,
    Guid ResourceId,
    string ResourceCode,
    string Action,
    string Code,
    string? Description
);

public record ResourceResponse(
    Guid Id,
    string Code,
    string? Description,
    IReadOnlyList<PermissionResponse> Permissions
);

public record RoleResponse(
    Guid Id,
    string Name,
    string? Description,
    bool IsSystemRole,
    Guid? StoreId,
    DateTimeOffset CreatedAt
);

public record RoleDetailResponse(
    Guid Id,
    string Name,
    string? Description,
    bool IsSystemRole,
    Guid? StoreId,
    IReadOnlyList<PermissionResponse> Permissions,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt
);
