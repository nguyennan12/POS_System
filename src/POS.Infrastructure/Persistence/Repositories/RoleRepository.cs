using Microsoft.EntityFrameworkCore;
using POS.Application.Abstractions.Persistence;
using POS.Domain.Rbac;

namespace POS.Infrastructure.Persistence.Repositories;

public class RoleRepository(AppDbContext context) : IRoleRepository
{
    public Task<Role?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.Roles.SingleOrDefaultAsync(r => r.Id == id, cancellationToken);
    public Task<List<Role>> GetSystemRolesByNameAsync(string name, CancellationToken cancellationToken = default) =>
        context.Roles.Where(r => r.IsSystemRole && r.StoreId == null && r.Name == name)
            .ToListAsync(cancellationToken);
}
