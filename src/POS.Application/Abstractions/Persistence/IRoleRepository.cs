using POS.Domain.Rbac;

namespace POS.Application.Abstractions.Persistence;

public interface IRoleRepository
{
    Task<Role?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<Role>> GetSystemRolesByNameAsync(string name, CancellationToken cancellationToken = default);
}
