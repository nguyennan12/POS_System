namespace POS.Application.UseCases.Rbac.Dtos;

public record ResourceDto(
    Guid Id,
    string Code,
    string? Description,
    IReadOnlyList<PermissionDto> Permissions
);
