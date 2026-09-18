using POS.Application.Abstractions.Messaging;
using POS.Application.Abstractions.Persistence;
using POS.Application.UseCases.Rbac.Dtos;
using POS.Domain.Common;

namespace POS.Application.UseCases.Rbac.Queries.GetRoles;

public class GetRolesQueryHandler(IRoleRepository roleRepository) : IQueryHandler<GetRolesQuery, IReadOnlyList<RoleDto>>
{
  public async Task<Result<IReadOnlyList<RoleDto>>> Handle(GetRolesQuery request, CancellationToken cancellationToken)
  {
    var roles = await roleRepository.GetRolesAsync(request.StoreId, cancellationToken);

    var dtos = roles.Select(r => new RoleDto(
        r.Id,
        r.Name,
        r.Description,
        r.IsSystemRole,
        r.StoreId,
        r.CreatedAt
    )).ToList();

    return dtos;
  }
}
