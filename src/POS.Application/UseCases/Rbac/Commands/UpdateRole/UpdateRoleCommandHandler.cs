using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;
using POS.Application.Abstractions.Persistence;
using POS.Application.UseCases.Rbac.Dtos;
using POS.Domain.Common;
using POS.Domain.Rbac.Errors;

namespace POS.Application.UseCases.Rbac.Commands.UpdateRole;

public class UpdateRoleCommandHandler(
    IRoleRepository roleRepository,
    IEmployeeStoreAccessRepository accessRepository,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork
) : ICommandHandler<UpdateRoleCommand, RoleDto>
{
    public async Task<Result<RoleDto>> Handle(UpdateRoleCommand request, CancellationToken cancellationToken)
    {
        var role = await roleRepository.GetByIdAsync(request.Id, cancellationToken);
        if (role == null)
        {
            return RoleErrors.NotFound;
        }

        if (role.IsSystemRole)
        {
            return RoleErrors.SystemRoleCannotBeModified;
        }

        var canManage = await RoleAccessControl.CanManageStoreAsync(
            currentUser, role.StoreId, accessRepository, cancellationToken);
        if (!canManage)
        {
            return RoleErrors.Forbidden;
        }

        var exists = await roleRepository.ExistsByNameAsync(request.Name, role.StoreId, role.Id, cancellationToken);
        if (exists)
        {
            return RoleErrors.NameAlreadyExists;
        }

        role.Update(request.Name.Trim(), request.Description?.Trim());
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
