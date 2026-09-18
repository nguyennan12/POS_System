using POS.Application.Abstractions.Auth;
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
    IEmployeeStoreAccessRepository accessRepository,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork
) : ICommandHandler<CreateRoleCommand, RoleDto>
{
    public async Task<Result<RoleDto>> Handle(CreateRoleCommand request, CancellationToken cancellationToken)
    {
        var canManage = await RoleAccessControl.CanManageStoreAsync(
            currentUser, request.StoreId, accessRepository, cancellationToken);
        if (!canManage)
        {
            return RoleErrors.Forbidden;
        }

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

        try
        {
            await roleRepository.AddAsync(role, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (PersistenceConflictException ex) when (ex.ConstraintName == PersistenceConstraints.RoleStoreNameUnique ||
                                                     ex.ConstraintName == PersistenceConstraints.RoleSystemNameUnique)
        {
            return RoleErrors.NameAlreadyExists;
        }

        return new RoleDto(
            role.Id,
            role.Name,
            role.Description,
            role.IsSystemRole,
            role.StoreId,
            role.CreatedAt);
    }
}
