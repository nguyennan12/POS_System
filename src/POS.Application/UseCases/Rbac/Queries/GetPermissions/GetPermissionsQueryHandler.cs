using POS.Application.Abstractions.Messaging;
using POS.Application.Abstractions.Persistence;
using POS.Application.UseCases.Rbac.Dtos;
using POS.Domain.Common;

namespace POS.Application.UseCases.Rbac.Queries.GetPermissions;

public class GetPermissionsQueryHandler(IPermissionRepository permissionRepository) : IQueryHandler<GetPermissionsQuery, IReadOnlyList<PermissionDto>>
{
  public async Task<Result<IReadOnlyList<PermissionDto>>> Handle(GetPermissionsQuery request, CancellationToken cancellationToken)
  {
    var permissions = await permissionRepository.GetAllAsync(request.ResourceId, cancellationToken);

    var dtos = permissions.Select(p => new PermissionDto(
        p.Id,
        p.ResourceId,
        p.Resource?.Code ?? string.Empty,
        p.Action.ToString(),
        p.Code,
        p.Description
    )).ToList();

    return dtos;
  }
}
