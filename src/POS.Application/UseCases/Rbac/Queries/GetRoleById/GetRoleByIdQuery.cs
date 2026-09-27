using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;
using POS.Application.UseCases.Rbac.Dtos;

namespace POS.Application.UseCases.Rbac.Queries.GetRoleById;

public record GetRoleByIdQuery(Guid Id) : IQuery<RoleDetailDto>, IRequirePermission
{
  public string RequiredPermission => "roles:read";
}
