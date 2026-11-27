using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using POS.Application.Abstractions.AI;
using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Caching;
using POS.Application.Abstractions.Persistence;
using POS.Application.UseCases.AI.Tools.Common;
using POS.Domain.Employees;

namespace POS.Application.Tests.AI;

internal sealed class AiToolTestHost(params string[] permissions)
{
    public Guid StoreId { get; } = Guid.NewGuid();
    public Guid EmployeeId { get; } = Guid.NewGuid();
    public ICurrentUser CurrentUser { get; } = Substitute.For<ICurrentUser>();
    public IEmployeeRepository Employees { get; } = Substitute.For<IEmployeeRepository>();

    public IServiceCollection CreateServices()
    {
        CurrentUser.IsAuthenticated.Returns(true);
        CurrentUser.EmployeeId.Returns(EmployeeId);
        CurrentUser.StoreId.Returns(StoreId);
        Employees.GetByIdAsync(EmployeeId, Arg.Any<CancellationToken>()).Returns(
            new Employee("Nhân viên", "employee", "hash", "pin", Guid.NewGuid(), storeId: StoreId, id: EmployeeId));

        var cache = Substitute.For<ICacheService>();
        cache.GetAsync<string[]>($"perm:{EmployeeId}", Arg.Any<CancellationToken>()).Returns(permissions);

        return new ServiceCollection()
            .AddSingleton(typeof(ILogger<>), typeof(NullLogger<>))
            .AddApplication()
            .AddSingleton(CurrentUser)
            .AddSingleton(Employees)
            .AddSingleton(cache)
            .AddSingleton(Substitute.For<IPermissionRepository>());
    }

    public static async Task<AiToolExecutionResult> ExecuteAsync(IServiceProvider provider, string name, string arguments)
    {
        using var scope = provider.CreateScope();
        using var json = JsonDocument.Parse(arguments);
        var executor = scope.ServiceProvider.GetRequiredService<AiToolExecutor>();
        var response = await executor.ExecuteAsync(new AiToolCall("call", name, json.RootElement), CancellationToken.None);
        return response.Result;
    }
}
