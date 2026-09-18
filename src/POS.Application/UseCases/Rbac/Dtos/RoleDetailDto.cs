namespace POS.Application.UseCases.Rbac.Dtos;

public record RoleDetailDto(
    Guid Id,
    string Name,
    string? Description,
    bool IsSystemRole,
    Guid? StoreId,
    IReadOnlyList<PermissionDto> Permissions,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt
);
