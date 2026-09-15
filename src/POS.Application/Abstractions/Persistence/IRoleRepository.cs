using POS.Domain.Rbac;

namespace POS.Application.Abstractions.Persistence;

public interface IRoleRepository
{
    Task<List<Role>> GetSystemRolesByNameAsync(string name, CancellationToken cancellationToken = default);
}
