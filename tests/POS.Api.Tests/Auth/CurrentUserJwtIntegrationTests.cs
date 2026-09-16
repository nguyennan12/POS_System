using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
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

public class CurrentUserJwtIntegrationTests
{
    [Theory]
    [InlineData(false, true)]
    [InlineData(true, true)]
    [InlineData(false, false)]
    [InlineData(true, false)]
    public async Task Password_login_token_resolves_current_user_through_real_bearer_middleware(
        bool isChainOwner, bool mapInboundClaims)
    {
        await using var factory = new LoginApplicationFactory(isChainOwner, mapInboundClaims);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false
        });

        using var anonymous = await client.GetAsync("/test/current-user");
        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "invalid-token");
        using var invalid = await client.GetAsync("/test/current-user");
        Assert.Equal(HttpStatusCode.Unauthorized, invalid.StatusCode);
        client.DefaultRequestHeaders.Authorization = null;

        using var wrongPassword = await client.PostAsJsonAsync("/api/v1/auth/employee/login",
            new LoginRequest(factory.Employee.Username, "Incorrect-password"));
        Assert.Equal(HttpStatusCode.Unauthorized, wrongPassword.StatusCode);

        using var login = await client.PostAsJsonAsync("/api/v1/auth/employee/login",
            new LoginRequest(factory.Employee.Username, LoginApplicationFactory.Password));
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var envelope = await login.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>();
        Assert.NotNull(envelope);
        Assert.True(envelope.Success);
        var auth = Assert.IsType<AuthResponse>(envelope.Data);
        Assert.Equal(factory.Employee.Id, auth.User.Id);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(auth.AccessToken);
        Assert.Equal(factory.Employee.Id.ToString(), jwt.Subject);
        Assert.DoesNotContain(jwt.Claims, claim => claim.Type == "employee_id");
        Assert.Equal(factory.Employee.RoleId.ToString(), jwt.Claims.Single(claim => claim.Type == "role_id").Value);
        Assert.Equal(factory.Employee.IsChainOwner.ToString(),
            jwt.Claims.Single(claim => claim.Type == "is_chain_owner").Value);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        using var response = await client.GetAsync("/test/current-user");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var current = await response.Content.ReadFromJsonAsync<CurrentUserSnapshot>();
        Assert.NotNull(current);
        Assert.True(current.IsAuthenticated);
        Assert.Equal(factory.Employee.Id, current.EmployeeId);
        Assert.Equal(factory.Employee.StoreId, current.StoreId);
        Assert.Equal(factory.Employee.RoleId, current.RoleId);
        Assert.Equal(factory.Employee.IsChainOwner, current.IsChainOwner);
        Assert.Equal(LoginApplicationFactory.Permissions, current.Permissions);
        Assert.Equal("Employee", current.SubjectType);
        // Role is a legacy role-name property. The issuer supplies role_id, not a role name.
        Assert.Null(current.Role);
        Assert.Equal(mapInboundClaims, current.HasMappedSubject);
        Assert.Equal(!mapInboundClaims, current.HasRawSubject);

        var options = factory.Services.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
            .Get(JwtBearerDefaults.AuthenticationScheme);
        Assert.Equal(mapInboundClaims, options.MapInboundClaims);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Signed_token_with_empty_role_id_resolves_null_without_exception(bool mapInboundClaims)
    {
        await using var factory = new LoginApplicationFactory(false, mapInboundClaims);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false
        });
        using var scope = factory.Services.CreateScope();
        var tokenService = scope.ServiceProvider.GetRequiredService<ITokenService>();
        var accessToken = tokenService.CreateAccessToken(new TokenSubject(
            factory.Employee.Id, "Employee", null, null, false, [])).AccessToken;
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(accessToken);
        Assert.Equal(string.Empty, jwt.Claims.Single(claim => claim.Type == "role_id").Value);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        using var response = await client.GetAsync("/test/current-user");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var current = await response.Content.ReadFromJsonAsync<CurrentUserSnapshot>();
        Assert.NotNull(current);
        Assert.True(current.IsAuthenticated);
        Assert.Equal(factory.Employee.Id, current.EmployeeId);
        Assert.Null(current.RoleId);
        Assert.False(current.IsChainOwner);
        Assert.Null(current.Role);
    }

    [Fact]
    public void Missing_or_malformed_role_and_chain_claims_use_safe_defaults()
    {
        var context = new DefaultHttpContext();
        var current = new POS.Api.Auth.CurrentUser(new HttpContextAccessor { HttpContext = context });

        context.User = new ClaimsPrincipal(new ClaimsIdentity([], "Bearer"));
        Assert.Null(current.RoleId);
        Assert.False(current.IsChainOwner);

        context.User = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim("role_id", "not-a-guid"),
            new Claim("is_chain_owner", "not-a-bool")
        ], "Bearer"));
        Assert.Null(current.RoleId);
        Assert.False(current.IsChainOwner);
    }

    [Fact]
    public async Task Application_registers_exactly_one_current_user_implementation()
    {
        await using var factory = new LoginApplicationFactory(false, true);
        using var scope = factory.Services.CreateScope();
        var registrations = scope.ServiceProvider.GetServices<ICurrentUser>().ToArray();
        var current = Assert.Single(registrations);
        Assert.IsType<POS.Api.Auth.CurrentUser>(current);
        Assert.Same(current, scope.ServiceProvider.GetRequiredService<ICurrentUser>());
    }

    private sealed class LoginApplicationFactory : WebApplicationFactory<AuthController>
    {
        internal const string Password = "Integration-password-123!";
        internal static readonly string[] Permissions = ["employees:read", "stores:read"];
        private static readonly string PasswordHash = new PasswordHasher().Hash(Password);
        private readonly bool mapInboundClaims;
        internal Employee Employee { get; }

        internal LoginApplicationFactory(bool isChainOwner, bool mapInboundClaims)
        {
            this.mapInboundClaims = mapInboundClaims;
            var role = new Role("Integration role", isSystemRole: true);
            Employee = new Employee("Integration employee", "integration-user", PasswordHash,
                "unused-by-password-login", role.Id, isChainOwner,
                storeId: isChainOwner ? null : Guid.NewGuid());
            typeof(Employee).GetProperty(nameof(Employee.Role))!.SetValue(Employee, role);
        }

        protected override IHost CreateHost(IHostBuilder builder)
        {
            // Supply configuration before the minimal Program reads it during service registration.
            builder.ConfigureHostConfiguration(configuration => configuration.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["Jwt:Secret"] = "Integration-only-signing-key-at-least-32-bytes-long",
                    ["Jwt:Issuer"] = "pos-integration-tests",
                    ["Jwt:Audience"] = "pos-integration-client",
                    ["Jwt:AccessTokenExpiryHours"] = "1",
                    ["Jwt:RefreshTokenExpiryDays"] = "1",
                    ["Auth:PinLookupSecret"] = "integration-only-pin-lookup-secret",
                    ["ConnectionStrings:Default"] = "Host=localhost;Database=unused_integration;Username=unused;Password=unused",
                    ["Redis:ConnectionString"] = "localhost:6379",
                    ["Serilog:LokiUrl"] = ""
                }));
            return base.CreateHost(builder);
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            // Run Program.cs, its DI and JWT settings; never migrate/seed an external database.
            builder.UseEnvironment("Testing");
            builder.ConfigureTestServices(services =>
            {
                // Only persistence boundaries are substituted. Controller, MediatR, validator,
                // login handler, BCrypt, TokenService, JWT authentication and CurrentUser are real.
                var employees = Substitute.For<IEmployeeRepository>();
                employees.GetByUsernameWithRoleAndStoreAsync(Employee.Username, Arg.Any<CancellationToken>())
                    .Returns(Employee);
                var permissions = Substitute.For<IPermissionRepository>();
                permissions.GetPermissionCodesAsync(Employee.RoleId, Arg.Any<CancellationToken>())
                    .Returns(Permissions);
                services.RemoveAll<IEmployeeRepository>();
                services.AddSingleton(employees);
                services.RemoveAll<IPermissionRepository>();
                services.AddSingleton(permissions);
                services.RemoveAll<IRefreshTokenRepository>();
                services.AddSingleton(Substitute.For<IRefreshTokenRepository>());
                services.RemoveAll<IUnitOfWork>();
                services.AddSingleton(Substitute.For<IUnitOfWork>());
                services.AddControllers().AddApplicationPart(typeof(CurrentUserProbeController).Assembly);
                // The true cases retain production defaults; false also verifies raw sub support.
                if (!mapInboundClaims)
                    services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme,
                        options => options.MapInboundClaims = false);
            });
        }
    }
}

// This probe is loaded only by the test host. It adds no production endpoint or contract.
[ApiController]
[Authorize]
[Route("test/current-user")]
public sealed class CurrentUserProbeController(ICurrentUser currentUser) : ControllerBase
{
    [HttpGet]
    public CurrentUserSnapshot Get() => new(
        currentUser.EmployeeId, currentUser.StoreId, currentUser.Role, currentUser.IsAuthenticated,
        currentUser.RoleId, currentUser.IsChainOwner,
        User.FindAll("permission").Select(claim => claim.Value).ToArray(),
        User.FindFirstValue("subject_type")!,
        User.HasClaim(claim => claim.Type == ClaimTypes.NameIdentifier),
        User.HasClaim(claim => claim.Type == JwtRegisteredClaimNames.Sub));
}

public sealed record CurrentUserSnapshot(Guid? EmployeeId, Guid? StoreId, string? Role,
    bool IsAuthenticated, Guid? RoleId, bool IsChainOwner, string[] Permissions, string SubjectType,
    bool HasMappedSubject, bool HasRawSubject);
