using Microsoft.Extensions.Logging;
using POS.Application.Abstractions.Caching;
using POS.Application.Abstractions.Persistence;
using POS.Domain.Auditing;
using POS.Domain.Common;
using POS.Domain.Employees;
using POS.Domain.Rbac;

namespace POS.Application.UseCases.Employees;

internal static class EmployeeMutationSupport
{
    public static async Task<Error> ValidateStoreAsync(Guid? storeId, bool isChainOwner,
        IStoreRepository stores, CancellationToken cancellationToken)
    {
        if (storeId is null) return isChainOwner ? Error.None : EmployeeErrors.InvalidStore;
        var store = await stores.GetByIdAsync(storeId.Value, cancellationToken);
        return store is { IsActive: true } ? Error.None : EmployeeErrors.InvalidStore;
    }

    public static bool RoleMatchesStore(Role role, Guid? storeId) =>
        role.StoreId == null || role.StoreId == storeId;

    public static async Task<EmployeeDto> SaveAsync(Employee target, Guid actorId, string action,
        IEmployeeRepository employees, IAuditLogRepository audits, IUnitOfWork unitOfWork, CancellationToken cancellationToken)
    {
        await audits.AddAsync(new AuditLog(actorId, target.Id, target.StoreId, action), cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return (await employees.GetDetailAsync(target.Id, cancellationToken))!.ToDto();
    }

    public static async Task InvalidateAsync(Guid targetId, ICacheService cache, ILogger logger,
        CancellationToken cancellationToken)
    {
        try
        {
            await cache.RemoveAsync($"perm:{targetId}", cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Employee mutation committed; permission cache invalidation failed for employee {EmployeeId}", targetId);
        }
    }
}
