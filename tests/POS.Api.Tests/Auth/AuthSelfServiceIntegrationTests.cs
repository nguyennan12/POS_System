using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using NSubstitute;
using POS.Api.Controllers;
using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Persistence;
using POS.Contracts.V1.Auth;
using POS.Contracts.V1.Common;
using POS.Domain.Employees;
using POS.Domain.Rbac;
using POS.Infrastructure.Auth;

namespace POS.Api.Tests.Auth;

public class AuthSelfServiceIntegrationTests
{
    [Fact]
    public async Task GetMe_returns_current_user_profile_and_permissions()
    {
        await using var factory = new SelfServiceApplicationFactory();
        using var client = factory.CreateClient();

        // 1. Anonymous -> 401
        using var anonResponse = await client.GetAsync("/api/v1/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, anonResponse.StatusCode);

        // 2. Login
        using var login = await client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest(factory.Employee.Username, SelfServiceApplicationFactory.Password));
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var envelope = await login.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>();
        var token = envelope!.Data!.AccessToken;

        // 3. Get /auth/me with Bearer token
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var meResponse = await client.GetAsync("/api/v1/auth/me");
        Assert.Equal(HttpStatusCode.OK, meResponse.StatusCode);

        var meEnvelope = await meResponse.Content.ReadFromJsonAsync<ApiResponse<CurrentUserResponse>>();
        Assert.NotNull(meEnvelope);
        Assert.True(meEnvelope.Success);
        Assert.Equal(factory.Employee.Id, meEnvelope.Data!.Id);
        Assert.Equal(factory.Employee.Name, meEnvelope.Data.Name);
        Assert.Equal(factory.Employee.Username, meEnvelope.Data.Username);
        Assert.Equal(factory.Employee.RoleId, meEnvelope.Data.RoleId);
        Assert.Equal(SelfServiceApplicationFactory.Permissions, meEnvelope.Data.Permissions);
    }

    [Fact]
    public async Task ChangePassword_validates_old_password_and_updates_successfully()
    {
        await using var factory = new SelfServiceApplicationFactory();
        using var client = factory.CreateClient();

        // Login
        using var login = await client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest(factory.Employee.Username, SelfServiceApplicationFactory.Password));
        var envelope = await login.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", envelope!.Data!.AccessToken);

        // Wrong old password -> 401
        using var wrongOld = await client.PostAsJsonAsync("/api/v1/auth/change-password",
            new ChangePasswordRequest("WrongOldPassword", "NewPassword-123!"));
        Assert.Equal(HttpStatusCode.Unauthorized, wrongOld.StatusCode);

        // Valid change password -> 204
        using var successChange = await client.PostAsJsonAsync("/api/v1/auth/change-password",
            new ChangePasswordRequest(SelfServiceApplicationFactory.Password, "NewPassword-123!"));
        Assert.Equal(HttpStatusCode.NoContent, successChange.StatusCode);

        // Verify password hash updated on entity
        var passwordHasher = new PasswordHasher();
        Assert.True(passwordHasher.Verify("NewPassword-123!", factory.Employee.PasswordHash));
    }

    [Fact]
    public async Task ChangePin_validates_old_pin_and_updates_successfully()
    {
        await using var factory = new SelfServiceApplicationFactory();
        using var client = factory.CreateClient();

        // Login
        using var login = await client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest(factory.Employee.Username, SelfServiceApplicationFactory.Password));
        var envelope = await login.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", envelope!.Data!.AccessToken);

        // Invalid Pin format -> 400
        using var invalidFormat = await client.PostAsJsonAsync("/api/v1/auth/change-pin",
            new ChangePinRequest("123", "abc"));
        Assert.Equal(HttpStatusCode.BadRequest, invalidFormat.StatusCode);

        // Wrong old pin -> 401
        using var wrongOldPin = await client.PostAsJsonAsync("/api/v1/auth/change-pin",
            new ChangePinRequest("999999", "654321"));
        Assert.Equal(HttpStatusCode.Unauthorized, wrongOldPin.StatusCode);

        // Valid change pin -> 204
        using var successChange = await client.PostAsJsonAsync("/api/v1/auth/change-pin",
            new ChangePinRequest(SelfServiceApplicationFactory.Pin, "654321"));
        Assert.Equal(HttpStatusCode.NoContent, successChange.StatusCode);

        // Verify pin hash updated on entity
        var passwordHasher = new PasswordHasher();
        Assert.True(passwordHasher.Verify("654321", factory.Employee.PinHash));
    }

    private sealed class SelfServiceApplicationFactory : WebApplicationFactory<AuthController>
    {
        internal const string Password = "Password-123!";
        internal const string Pin = "123456";
        internal static readonly string[] Permissions = ["employees:read", "orders:create"];
        private static readonly string PasswordHash = new PasswordHasher().Hash(Password);
        private static readonly string PinHash = new PasswordHasher().Hash(Pin);
        internal Employee Employee { get; }

        internal SelfServiceApplicationFactory()
        {
            var role = new Role("Cashier Role", isSystemRole: true);
            Employee = new Employee("Self Service User", "self-service-user", PasswordHash,
                PinHash, role.Id, isChainOwner: false, storeId: Guid.NewGuid());
            typeof(Employee).GetProperty(nameof(Employee.Role))!.SetValue(Employee, role);
        }

        protected override IHost CreateHost(IHostBuilder builder)
        {
            builder.ConfigureHostConfiguration(configuration => configuration.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["Jwt:Secret"] = "Integration-only-signing-key-at-least-32-bytes-long",
                    ["Jwt:Issuer"] = "pos-integration-tests",
                    ["Jwt:Audience"] = "pos-integration-client",
                    ["Jwt:AccessTokenExpiryHours"] = "1",
                    ["Jwt:RefreshTokenExpiryDays"] = "1",
                    ["Auth:PinLookupSecret"] = "integration-only-pin-lookup-secret",
                    ["ConnectionStrings:Default"] = "Host=localhost;Database=unused;Username=unused;Password=unused",
                    ["Redis:ConnectionString"] = "localhost:6379",
                    ["Serilog:LokiUrl"] = ""
                }));
            return base.CreateHost(builder);
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureTestServices(services =>
            {
                var employees = Substitute.For<IEmployeeRepository>();
                employees.GetByUsernameWithRoleAndStoreAsync(Employee.Username, Arg.Any<CancellationToken>())
                    .Returns(Employee);
                employees.GetDetailAsync(Employee.Id, Arg.Any<CancellationToken>())
                    .Returns(Employee);
                employees.HasPinConflictAsync(Employee.Id, Employee.StoreId, Arg.Any<string>(), Arg.Any<CancellationToken>())
                    .Returns(false);

                var permissions = Substitute.For<IPermissionRepository>();
                permissions.GetPermissionCodesAsync(Employee.RoleId, Arg.Any<CancellationToken>())
                    .Returns(Permissions);

                services.RemoveAll<IEmployeeRepository>();
                services.AddSingleton(employees);
                services.RemoveAll<IPermissionRepository>();
                services.AddSingleton(permissions);
                services.RemoveAll<IRefreshTokenRepository>();
                services.AddSingleton(Substitute.For<IRefreshTokenRepository>());
                services.RemoveAll<IAuditLogRepository>();
                services.AddSingleton(Substitute.For<IAuditLogRepository>());
                services.RemoveAll<IUnitOfWork>();
                services.AddSingleton(Substitute.For<IUnitOfWork>());
            });
        }
    }
}
