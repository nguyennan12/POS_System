using MediatR;
using NSubstitute;
using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Caching;
using POS.Application.Abstractions.Persistence;
using POS.Application.Common.Behaviors;
using POS.Domain.Common;
using POS.Domain.Employees;
using POS.Domain.Rbac;

namespace POS.Application.Tests.Common.Behaviors;

public class AuthorizationBehaviorTests
{
    private readonly ICurrentUser currentUser = Substitute.For<ICurrentUser>();
    private readonly ICacheService cache = Substitute.For<ICacheService>();
    private readonly IEmployeeRepository employees = Substitute.For<IEmployeeRepository>();
    private readonly IPermissionRepository permissions = Substitute.For<IPermissionRepository>();
    private readonly IServiceProvider services = Substitute.For<IServiceProvider>();
    private readonly Employee employee;
    private readonly AuthorizationBehavior<AuthorizedRequest, Result<string>> behavior;

    public AuthorizationBehaviorTests()
    {
        var role = new Role("Manager");
        employee = new Employee("Employee", "employee", "hash", "hash", role.Id);
        typeof(Employee).GetProperty(nameof(Employee.Role))!.SetValue(employee, role);

        currentUser.IsAuthenticated.Returns(true);
        currentUser.EmployeeId.Returns(employee.Id);
        employees.GetByIdAsync(employee.Id, Arg.Any<CancellationToken>()).Returns(employee);

        services.GetService(typeof(ICacheService)).Returns(cache);
        services.GetService(typeof(IEmployeeRepository)).Returns(employees);
        services.GetService(typeof(IPermissionRepository)).Returns(permissions);
        behavior = new(currentUser, services);
    }

    [Fact]
    public async Task Missing_permission_returns_forbidden_without_calling_handler_or_database()
    {
        cache.GetAsync<string[]>($"perm:{employee.Id}", Arg.Any<CancellationToken>())
            .Returns(["stores:read"]);
        var handlerCalled = false;

        var result = await behavior.Handle(
            new AuthorizedRequest("stores:update"),
            () =>
            {
                handlerCalled = true;
                return Task.FromResult(Result<string>.Success("ok"));
            },
            default);

        Assert.Equal(ErrorType.Forbidden, result.Error.Type);
        Assert.False(handlerCalled);
        await employees.DidNotReceive().GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await permissions.DidNotReceive().GetPermissionCodesAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Cache_miss_queries_role_permissions_and_caches_for_five_minutes()
    {
        cache.GetAsync<string[]>($"perm:{employee.Id}", Arg.Any<CancellationToken>())
            .Returns((string[]?)null);
        permissions.GetPermissionCodesAsync(employee.RoleId, Arg.Any<CancellationToken>())
            .Returns(["stores:update"]);

        var result = await behavior.Handle(
            new AuthorizedRequest("stores:update"),
            () => Task.FromResult(Result<string>.Success("ok")),
            default);

        Assert.True(result.IsSuccess);
        await permissions.Received(1).GetPermissionCodesAsync(employee.RoleId, Arg.Any<CancellationToken>());
        await cache.Received(1).SetAsync(
            $"perm:{employee.Id}",
            Arg.Is<string[]>(codes => codes.Length == 1 && codes[0] == "stores:update"),
            TimeSpan.FromMinutes(5),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Invalid_current_user_returns_unauthorized()
    {
        currentUser.IsAuthenticated.Returns(false);

        var result = await behavior.Handle(
            new AuthorizedRequest("stores:update"),
            () => Task.FromResult(Result<string>.Success("ok")),
            default);

        Assert.Equal(ErrorType.Unauthorized, result.Error.Type);
    }

    [Fact]
    public async Task Request_without_permission_requirement_bypasses_authorization()
    {
        var bypassBehavior = new AuthorizationBehavior<UnprotectedRequest, Result<string>>(
            currentUser, services);

        var result = await bypassBehavior.Handle(
            new UnprotectedRequest(),
            () => Task.FromResult(Result<string>.Success("ok")),
            default);

        Assert.True(result.IsSuccess);
        await cache.DidNotReceive().GetAsync<string[]>(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    private sealed record AuthorizedRequest(string RequiredPermission)
        : IRequest<Result<string>>, IRequirePermission;

    private sealed record UnprotectedRequest : IRequest<Result<string>>;
}