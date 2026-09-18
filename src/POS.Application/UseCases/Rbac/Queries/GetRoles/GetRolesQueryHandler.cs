using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;
using POS.Application.Abstractions.Persistence;
using POS.Application.UseCases.Rbac.Dtos;
using POS.Domain.Common;
using POS.Domain.Rbac.Errors;

namespace POS.Application.UseCases.Rbac.Queries.GetRoles;

public class GetRolesQueryHandler(
    IRoleRepository roleRepository,
    IEmployeeStoreAccessRepository accessRepository,
    ICurrentUser currentUser
) : IQueryHandler<GetRolesQuery, IReadOnlyList<RoleDto>>
{
    public async Task<Result<IReadOnlyList<RoleDto>>> Handle(GetRolesQuery request, CancellationToken cancellationToken)
    {
        Guid? effectiveStoreId = request.StoreId;

        if (!currentUser.IsChainOwner)
        {
            if (request.StoreId.HasValue)
            {
                var canAccess = currentUser.StoreId == request.StoreId.Value ||
                    (currentUser.EmployeeId.HasValue &&
                     await accessRepository.ExistsAsync(currentUser.EmployeeId.Value, request.StoreId.Value, cancellationToken));

                if (!canAccess)
                {
                    return RoleErrors.Forbidden;
                }
            }
            else
            {
                effectiveStoreId = currentUser.StoreId;
            }
        }

        var roles = await roleRepository.GetRolesAsync(effectiveStoreId, cancellationToken);

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
