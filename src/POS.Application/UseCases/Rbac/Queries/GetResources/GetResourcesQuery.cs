using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;
using POS.Application.UseCases.Rbac.Dtos;

namespace POS.Application.UseCases.Rbac.Queries.GetResources;

public record GetResourcesQuery : IQuery<IReadOnlyList<ResourceDto>>, IRequirePermission
{
  public string RequiredPermission => "roles:read";
}
