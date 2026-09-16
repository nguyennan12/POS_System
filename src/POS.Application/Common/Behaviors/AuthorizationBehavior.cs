using MediatR;
using Microsoft.Extensions.DependencyInjection;
using POS.Application.Abstractions.Auth;
using POS.Domain.Common;

namespace POS.Application.Common.Behaviors;

public sealed class AuthorizationBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : Result
{
    private static readonly TimeSpan PermissionCacheDuration = TimeSpan.FromMinutes(5);
    private readonly ICurrentUser currentUser;
    private readonly IServiceProvider services;

    public AuthorizationBehavior(
        ICurrentUser currentUser,
        IServiceProvider services)
    {
        this.currentUser = currentUser;
        this.services = services;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (request is not IRequirePermission permissionRequest)
            return await next();

        if (!currentUser.IsAuthenticated || currentUser.EmployeeId is not Guid employeeId)
            return CreateFailure<TResponse>(
                new Error(ErrorType.Unauthorized, "Authorization.Unauthorized", "Cần đăng nhập với danh tính nhân viên hợp lệ."));

        var cache = services.GetRequiredService<POS.Application.Abstractions.Caching.ICacheService>();
        var employees = services.GetRequiredService<POS.Application.Abstractions.Persistence.IEmployeeRepository>();
        var permissions = services.GetRequiredService<POS.Application.Abstractions.Persistence.IPermissionRepository>();
        var cacheKey = $"perm:{employeeId}";
        var permissionCodes = await cache.GetAsync<string[]>(cacheKey, cancellationToken);

        if (permissionCodes is null)
        {
            var employee = await employees.GetByIdAsync(employeeId, cancellationToken);
            if (employee is null || !employee.IsActive || employee.IsLocked(DateTime.UtcNow))
            {
                return CreateFailure<TResponse>(
                    new Error(ErrorType.Unauthorized, "Authorization.Unauthorized", "Danh tính nhân viên không còn hợp lệ."));
            }

            permissionCodes = (await permissions.GetPermissionCodesAsync(employee.RoleId, cancellationToken)).ToArray();
            await cache.SetAsync(cacheKey, permissionCodes, PermissionCacheDuration, cancellationToken);
        }

        if (!permissionCodes.Contains(permissionRequest.RequiredPermission, StringComparer.OrdinalIgnoreCase))
        {
            return CreateFailure<TResponse>(
                new Error(ErrorType.Forbidden, "Authorization.Forbidden", "Bạn không có quyền thực hiện thao tác này."));
        }

        return await next();
    }

    private static TResult CreateFailure<TResult>(Error error)
        where TResult : Result
    {
        if (typeof(TResult) == typeof(Result))
            return (Result.Failure(error) as TResult)!;

        var resultType = typeof(TResult).GenericTypeArguments[0];
        var failure = typeof(Result<>).MakeGenericType(resultType)
            .GetMethod(nameof(Result<object>.Failure))!
            .Invoke(null, [error])!;

        return (TResult)failure;
    }
}