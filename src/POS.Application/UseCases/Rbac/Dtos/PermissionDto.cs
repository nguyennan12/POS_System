namespace POS.Application.UseCases.Rbac.Dtos;

public record PermissionDto(
    Guid Id,
    Guid ResourceId,
    string ResourceCode,
    string Action,
    string Code,
    string? Description
);
