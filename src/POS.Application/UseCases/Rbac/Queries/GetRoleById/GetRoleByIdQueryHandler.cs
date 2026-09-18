using POS.Application.Abstractions.Messaging;
using POS.Application.Abstractions.Persistence;
using POS.Application.UseCases.Rbac.Dtos;
using POS.Domain.Common;
using POS.Domain.Rbac.Errors;

namespace POS.Application.UseCases.Rbac.Queries.GetRoleById;

public class GetRoleByIdQueryHandler(IRoleRepository roleRepository) : IQueryHandler<GetRoleByIdQuery, RoleDetailDto>
{
  public async Task<Result<RoleDetailDto>> Handle(GetRoleByIdQuery request, CancellationToken cancellationToken)
  {
    var role = await roleRepository.GetByIdWithPermissionsAsync(request.Id, cancellationToken);
    if (role == null)
    {
      return RoleErrors.NotFound;
    }

    var permissionDtos = role.RolePermissions
        .Where(rp => rp.Permission != null)
        .Select(rp => new PermissionDto(
            rp.Permission.Id,
            rp.Permission.ResourceId,
            rp.Permission.Resource?.Code ?? string.Empty,
            rp.Permission.Action.ToString(),
            rp.Permission.Code,
            rp.Permission.Description))
        .ToList();

    return new RoleDetailDto(
        role.Id,
        role.Name,
        role.Description,
        role.IsSystemRole,
        role.StoreId,
        permissionDtos,
        role.CreatedAt,
        role.UpdatedAt);
  }
}
