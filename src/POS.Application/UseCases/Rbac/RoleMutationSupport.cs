using Microsoft.Extensions.Logging;
using POS.Application.Abstractions.Caching;
using POS.Application.Abstractions.Persistence;

namespace POS.Application.UseCases.Rbac;

internal static class RoleMutationSupport
{
    public static async Task InvalidateRoleEmployeesAsync(
        Guid roleId,
        IRoleRepository roleRepository,
        ICacheService cacheService,
        ILogger logger)
    {
        try
        {
            var employeeIds = await roleRepository.GetEmployeeIdsByRoleIdAsync(roleId, CancellationToken.None);
            if (employeeIds.Count > 0)
            {
                var cacheKeys = employeeIds.Select(id => $"perm:{id}");
                await cacheService.RemoveRangeAsync(cacheKeys, CancellationToken.None);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Role permissions committed; Redis cache invalidation failed for role {RoleId}", roleId);
        }
    }
}
