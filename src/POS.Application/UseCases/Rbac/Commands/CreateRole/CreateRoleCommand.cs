using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;
using POS.Application.UseCases.Rbac.Dtos;

namespace POS.Application.UseCases.Rbac.Commands.CreateRole;

public record CreateRoleCommand(
    string Name,
    string? Description = null,
    Guid? StoreId = null
) : ICommand<RoleDto>, IRequirePermission
{
  public string RequiredPermission => "roles:create";
}
