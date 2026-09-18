using POS.Application.Abstractions.Messaging;
using POS.Application.Abstractions.Persistence;
using POS.Application.UseCases.Rbac.Dtos;
using POS.Domain.Common;
using POS.Domain.Rbac;
using POS.Domain.Rbac.Errors;

namespace POS.Application.UseCases.Rbac.Commands.CreateRole;

public class CreateRoleCommandHandler(
    IRoleRepository roleRepository,
    IStoreRepository storeRepository,
    IUnitOfWork unitOfWork
) : ICommandHandler<CreateRoleCommand, RoleDto>
{
  public async Task<Result<RoleDto>> Handle(CreateRoleCommand request, CancellationToken cancellationToken)
  {
    if (request.StoreId.HasValue)
    {
      var store = await storeRepository.GetByIdAsync(request.StoreId.Value, cancellationToken);
      if (store == null || !store.IsActive)
      {
        return RoleErrors.InvalidStore;
      }
    }

    var exists = await roleRepository.ExistsByNameAsync(request.Name, request.StoreId, null, cancellationToken);
    if (exists)
    {
      return RoleErrors.NameAlreadyExists;
    }

    var role = new Role(
        name: request.Name.Trim(),
        isSystemRole: false,
        storeId: request.StoreId,
        description: request.Description?.Trim());

    await roleRepository.AddAsync(role, cancellationToken);
    await unitOfWork.SaveChangesAsync(cancellationToken);

    return new RoleDto(
        role.Id,
        role.Name,
        role.Description,
        role.IsSystemRole,
        role.StoreId,
        role.CreatedAt);
  }
}
