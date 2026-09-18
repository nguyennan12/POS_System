using System.Collections.Concurrent;
using System.Data.Common;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Npgsql;
using NSubstitute;
using POS.Api.Controllers;
using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Caching;
using POS.Contracts.V1.Auth;
using POS.Contracts.V1.Common;
using POS.Domain.Employees;
using POS.Domain.Rbac;
using POS.Domain.Rbac.Constants;
using POS.Domain.Rbac.Enums;
using POS.Domain.Stores;
using POS.Infrastructure.Auth;
using POS.Infrastructure.Persistence;

namespace POS.Api.Tests.Employees;

public sealed class EmployeePostgresTheoryAttribute : TheoryAttribute
{
    public EmployeePostgresTheoryAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("POS_TEST_POSTGRES")))
            Skip = "Requires a dedicated POS_TEST_POSTGRES database; each case uses an isolated schema.";
    }
}

// Actual PostgreSQL, repositories, UoW, pipeline, BCrypt, login, TokenService and JWT middleware.
// Only Redis is replaced by an observable in-memory cache. No fabricated ClaimsPrincipal.
internal sealed class EmployeeApiFactory : WebApplicationFactory<AuthController>
{
    internal const string Password = "Employee-tests-password-123!";
    private static readonly string PasswordHash = new PasswordHasher().Hash(Password);
    private static readonly ConcurrentDictionary<string, string> PinHashes = new();
    private readonly string schema = "employee_test_" + Guid.NewGuid().ToString("N");
    private readonly string connection;
    internal readonly Store A = new("Store A");
    internal readonly Store B = new("Store B");
    internal readonly Store Inactive = new("Inactive", isActive: false);
    internal readonly Role Owner = new(RoleNames.Owner, true);
    internal readonly Role Manager = new(RoleNames.StoreManager, true);
    internal readonly Role Cashier = new(RoleNames.Cashier, true);
    internal Role Custom { get; }
    internal Dictionary<string, Employee> People { get; } = new();
    internal ObservableCache Cache { get; } = new();
    internal SnapshotBarrier Barrier { get; } = new();

    internal EmployeeApiFactory()
    {
        connection = new NpgsqlConnectionStringBuilder(Environment.GetEnvironmentVariable("POS_TEST_POSTGRES"))
        {
            SearchPath = schema,
            Pooling = false
        }.ConnectionString;
        Custom = new Role(RoleNames.Owner, storeId: A.Id);
    }

    internal AppDbContext Db() => new(new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(connection).Options);

    internal async Task InitializeAsync()
    {
        await using var admin = new NpgsqlConnection(connection);
        await admin.OpenAsync();
        await using var create = new NpgsqlCommand($"CREATE SCHEMA \"{schema}\"", admin);
        await create.ExecuteNonQueryAsync();
        await using var db = Db();
        await db.Database.ExecuteSqlRawAsync(db.Database.GenerateCreateScript());
        db.Stores.AddRange(A, B, Inactive);
        db.Roles.AddRange(Owner, Manager, Cashier, Custom);
        var resource = new Resource(ResourceNames.Employees);
        db.Resources.Add(resource);
        foreach (var action in new[] { PermissionAction.Create, PermissionAction.Read, PermissionAction.Update })
        {
            var permission = new Permission(resource.Id, resource, action);
            db.Permissions.Add(permission);
            // Deliberately grant codes even to low-level roles: business hierarchy must still reject mutations.
            foreach (var role in new[] { Owner, Manager, Cashier, Custom })
                db.RolePermissions.Add(new RolePermission(role.Id, role, permission.Id, permission));
        }
        Add("chain", Owner, A.Id, "100001", true);
        Add("chain2", Owner, A.Id, "100002", true);
        Add("owner", Owner, A.Id, "100003");
        Add("owner2", Owner, A.Id, "100004");
        Add("manager", Manager, A.Id, "100005");
        Add("cashier", Cashier, A.Id, "100006");
        Add("custom", Custom, A.Id, "100007");
        Add("ownerB", Owner, B.Id, "200003");
        Add("managerB", Manager, B.Id, "200005");
        Add("cashierB", Cashier, B.Id, "200006");
        Add("chainNull", Owner, null, "300001", true);
        db.Employees.AddRange(People.Values);
        await db.SaveChangesAsync();
    }

    private void Add(string key, Role role, Guid? storeId, string pin, bool chain = false)
    {
        var person = new Employee(key, key, PasswordHash,
            PinHashes.GetOrAdd(pin, value => new PasswordHasher().Hash(value)), role.Id, chain, storeId);
        person.setPinLookUpHash(new PinLookupHasher(new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?> { ["Auth:PinLookupSecret"] = "employee-test-pin-lookup-secret" }).Build()).ComputeHash(pin));
        People.Add(key, person);
    }

    internal HttpClient Client() => CreateClient(new WebApplicationFactoryClientOptions
    {
        BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false
    });

    internal async Task<AuthResponse> LoginAsync(HttpClient client, string person)
    {
        client.DefaultRequestHeaders.Authorization = null;
        using var response = await client.PostAsJsonAsync("/api/v1/auth/employee/login", new LoginRequest(person, Password));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var auth = (await response.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>())!.Data!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        return auth;
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.ConfigureHostConfiguration(c => c.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:Secret"] = "Employee-tests-only-signing-key-at-least-32-bytes",
            ["Jwt:Issuer"] = "employee-tests", ["Jwt:Audience"] = "employee-tests",
            ["Jwt:AccessTokenExpiryHours"] = "1", ["Jwt:RefreshTokenExpiryDays"] = "1",
            ["Auth:PinLookupSecret"] = "employee-test-pin-lookup-secret",
            ["ConnectionStrings:Default"] = connection,
            ["Redis:ConnectionString"] = "localhost:6379", ["Serilog:LokiUrl"] = ""
        }));
        return base.CreateHost(builder);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<ICacheService>();
            services.AddSingleton<ICacheService>(Cache);
            services.RemoveAll<IPinLoginRateLimiter>();
            services.AddSingleton(Substitute.For<IPinLoginRateLimiter>());
            services.AddDbContext<AppDbContext>(options => options.AddInterceptors(Barrier));
        });
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        await using var admin = new NpgsqlConnection(connection);
        await admin.OpenAsync();
        await using var drop = new NpgsqlCommand($"DROP SCHEMA IF EXISTS \"{schema}\" CASCADE", admin);
        await drop.ExecuteNonQueryAsync();
    }
}

internal sealed class ObservableCache : ICacheService
{
    private readonly ConcurrentDictionary<string, object> values = new();
    internal ConcurrentQueue<string> Removed { get; } = new();
    internal Func<string, Task>? OnRemove { get; set; }
    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) =>
        Task.FromResult(values.TryGetValue(key, out var value) ? (T?)value : default);
    public Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken cancellationToken = default)
    {
        values[key] = value!;
        return Task.CompletedTask;
    }
    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        if (OnRemove != null) await OnRemove(key);
        values.TryRemove(key, out _);
        Removed.Enqueue(key);
    }
}

internal sealed class SnapshotBarrier : DbCommandInterceptor
{
    internal string? Match { get; set; }
    internal int Hits;
    private readonly TaskCompletionSource ready = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public override async ValueTask<DbDataReader> ReaderExecutedAsync(DbCommand command,
        CommandExecutedEventData eventData, DbDataReader result, CancellationToken cancellationToken = default)
    {
        if (Match != null && command.CommandText.Contains("EXISTS") && command.CommandText.Contains(Match))
        {
            var hit = Interlocked.Increment(ref Hits);
            if (hit == 2) ready.TrySetResult();
            if (hit <= 2) await ready.Task.WaitAsync(TimeSpan.FromSeconds(20), cancellationToken);
        }
        return result;
    }
}
