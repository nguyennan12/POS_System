using POS.Application.Abstractions.Messaging;
using POS.Application.Abstractions.Persistence;
using POS.Application.UseCases.Rbac.Dtos;
using POS.Domain.Common;

namespace POS.Application.UseCases.Rbac.Queries.GetResources;

public class GetResourcesQueryHandler(IPermissionRepository permissionRepository) : IQueryHandler<GetResourcesQuery, IReadOnlyList<ResourceDto>>
{
  public async Task<Result<IReadOnlyList<ResourceDto>>> Handle(GetResourcesQuery request, CancellationToken cancellationToken)
  {
    var resources = await permissionRepository.GetResourcesWithPermissionsAsync(cancellationToken);

    var dtos = resources.Select(r => new ResourceDto(
        r.Id,
        r.Code,
        r.Description,
        r.Permissions.Select(p => new PermissionDto(
            p.Id,
            p.ResourceId,
            r.Code,
            p.Action.ToString(),
            p.Code,
            p.Description
        )).ToList()
    )).ToList();

    return dtos;
  }
}
