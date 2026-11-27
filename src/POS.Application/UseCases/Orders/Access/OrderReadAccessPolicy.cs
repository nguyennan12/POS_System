using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Caching;
using POS.Application.Abstractions.Persistence;
using POS.Application.UseCases.Orders.Errors;
using POS.Domain.Common;

namespace POS.Application.UseCases.Orders.Access;

public sealed class OrderReadAccessPolicy(
    ICurrentUser currentUser,
    IStoreAccessPolicy storeAccessPolicy,
    ICacheService cacheService,
    IEmployeeRepository employeeRepository,
    IPermissionRepository permissionRepository) : IOrderReadAccessPolicy
{
    private static readonly TimeSpan PermissionCacheDuration = TimeSpan.FromMinutes(5);

    public async Task<Result<OrderReadScope>> ResolveReadScopeAsync(CancellationToken cancellationToken = default)
    {
        if (!currentUser.IsAuthenticated || currentUser.EmployeeId is not Guid employeeId || employeeId == Guid.Empty)
        {
            return OrderErrors.Unauthorized;
        }

        var storeContext = storeAccessPolicy.EnforceStoreAccess();
        if (storeContext.IsFailure)
        {
            return storeContext.Error;
        }

        var cacheKey = $"perm:{employeeId}";
        var permissionCodes = await cacheService.GetAsync<string[]>(cacheKey, cancellationToken);

        if (permissionCodes is null)
        {
            var employee = await employeeRepository.GetByIdAsync(employeeId, cancellationToken);
            if (employee is null || !employee.IsActive || employee.IsLocked(DateTime.UtcNow))
            {
                return OrderErrors.Unauthorized;
            }

            permissionCodes = (await permissionRepository.GetPermissionCodesAsync(employee.RoleId, cancellationToken))
                .ToArray();
            await cacheService.SetAsync(cacheKey, permissionCodes, PermissionCacheDuration, cancellationToken);
        }

        if (permissionCodes.Contains("orders:read", StringComparer.OrdinalIgnoreCase))
        {
            return Result<OrderReadScope>.Success(new OrderReadScope(
                StoreId: storeContext.Value,
                EmployeeId: employeeId,
                CanReadAllInStore: true));
        }

        if (permissionCodes.Contains("orders:read_own", StringComparer.OrdinalIgnoreCase))
        {
            return Result<OrderReadScope>.Success(new OrderReadScope(
                StoreId: storeContext.Value,
                EmployeeId: employeeId,
                CanReadAllInStore: false));
        }

        return new Error(
            ErrorType.Forbidden,
            "Authorization.Forbidden",
            "Bạn không có quyền xem dữ liệu đơn hàng.");
    }
}
