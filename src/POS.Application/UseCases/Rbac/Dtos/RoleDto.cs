namespace POS.Application.UseCases.Rbac.Dtos;

public record RoleDto(
    Guid Id,
    string Name,
    string? Description,
    bool IsSystemRole,
    Guid? StoreId,
    DateTimeOffset CreatedAt
);
