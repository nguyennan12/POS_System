using POS.Application.UseCases.Rbac.Dtos;
using POS.Contracts.V1.Rbac;

namespace POS.Api.Mappings;

public static class RoleMapping
{
  public static RoleResponse ToResponse(this RoleDto dto)
  {
    return new RoleResponse(
        dto.Id,
        dto.Name,
        dto.Description,
        dto.IsSystemRole,
        dto.StoreId,
        dto.CreatedAt
    );
  }

  public static RoleDetailResponse ToDetailResponse(this RoleDetailDto dto)
  {
    return new RoleDetailResponse(
        dto.Id,
        dto.Name,
        dto.Description,
        dto.IsSystemRole,
        dto.StoreId,
        dto.Permissions.Select(p => p.ToResponse()).ToList(),
        dto.CreatedAt,
        dto.UpdatedAt
    );
  }

  public static PermissionResponse ToResponse(this PermissionDto dto)
  {
    return new PermissionResponse(
        dto.Id,
        dto.ResourceId,
        dto.ResourceCode,
        dto.Action,
        dto.Code,
        dto.Description
    );
  }

  public static ResourceResponse ToResponse(this ResourceDto dto)
  {
    return new ResourceResponse(
        dto.Id,
        dto.Code,
        dto.Description,
        dto.Permissions.Select(p => p.ToResponse()).ToList()
    );
  }
}
