using System.Net;
using System.Reflection;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NSubstitute;
using POS.Api.Controllers;
using POS.Api.Extensions;
using POS.Application.UseCases.Rbac.Commands.CreateRole;
using POS.Application.UseCases.Rbac.Commands.UpdateRole;
using POS.Application.UseCases.Rbac.Commands.UpdateRolePermissions;
using POS.Application.UseCases.Rbac.Dtos;
using POS.Application.UseCases.Rbac.Queries.GetPermissions;
using POS.Application.UseCases.Rbac.Queries.GetResources;
using POS.Application.UseCases.Rbac.Queries.GetRoleById;
using POS.Application.UseCases.Rbac.Queries.GetRolePermissions;
using POS.Application.UseCases.Rbac.Queries.GetRoles;
using POS.Contracts.V1.Rbac;
using POS.Domain.Common;

namespace POS.Api.Tests.Roles;

public class RolesControllerTests
{
  private static Task<IHost> CreateBindingHostAsync(ISender mediator) => new HostBuilder()
      .ConfigureWebHost(builder => builder.UseTestServer()
      .ConfigureServices(services =>
      {
        services.AddSingleton(mediator);
        services.AddAuthentication();
        services.AddAuthorization();
        services.AddControllers().AddApplicationPart(typeof(RolesController).Assembly);
      })
      .Configure(app =>
      {
        app.UseRouting();
        app.Use((context, next) =>
          {
            context.User = new ClaimsPrincipal(new ClaimsIdentity(
                  [new Claim("employee_id", Guid.NewGuid().ToString())], "BindingTest"));
            return next(context);
          });
        app.UseAuthorization();
        app.UseEndpoints(endpoints => endpoints.MapControllers());
      })).StartAsync();

  [Fact]
  public void Roles_controller_requires_authentication_and_has_correct_route()
  {
    Assert.NotNull(typeof(RolesController).GetCustomAttribute<AuthorizeAttribute>());
    Assert.Equal("api/v1/roles", typeof(RolesController).GetCustomAttribute<RouteAttribute>()!.Template);
  }

  [Theory]
  [InlineData(nameof(RolesController.GetRoles), "GET", null)]
  [InlineData(nameof(RolesController.GetById), "GET", "{id:guid}")]
  [InlineData(nameof(RolesController.Create), "POST", null)]
  [InlineData(nameof(RolesController.Update), "PUT", "{id:guid}")]
  [InlineData(nameof(RolesController.GetPermissionsByRoleId), "GET", "{id:guid}/permissions")]
  [InlineData(nameof(RolesController.UpdatePermissions), "PUT", "{id:guid}/permissions")]
  [InlineData(nameof(RolesController.GetResources), "GET", "/api/v1/resources")]
  [InlineData(nameof(RolesController.GetPermissions), "GET", "/api/v1/permissions")]
  public void Role_endpoints_have_correct_http_verbs_and_routes(string action, string verb, string? path)
  {
    var method = typeof(RolesController).GetMethod(action)!;
    var routeAttr = method.GetCustomAttribute<HttpMethodAttribute>()!;
    Assert.Contains(verb, routeAttr.HttpMethods);
    Assert.Equal(path, routeAttr.Template);
  }

  [Fact]
  public async Task GetRoles_DispatchesQuery_AndReturnsOk()
  {
    var mediator = Substitute.For<ISender>();
    var roles = new List<RoleDto>
        {
            new(Guid.NewGuid(), "Owner", null, true, null, DateTimeOffset.UtcNow),
            new(Guid.NewGuid(), "Cashier", null, true, null, DateTimeOffset.UtcNow)
        };
    mediator.Send(Arg.Any<GetRolesQuery>(), Arg.Any<CancellationToken>())
        .Returns(roles);

    using var host = await CreateBindingHostAsync(mediator);
    using var client = host.GetTestClient();
    using var response = await client.GetAsync("/api/v1/roles");

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    await mediator.Received(1).Send(Arg.Any<GetRolesQuery>(), Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task CreateRole_DispatchesCommand_AndReturnsCreated()
  {
    var roleId = Guid.NewGuid();
    var mediator = Substitute.For<ISender>();
    var roleDto = new RoleDto(roleId, "Custom Role", "Description", false, null, DateTimeOffset.UtcNow);
    mediator.Send(Arg.Any<CreateRoleCommand>(), Arg.Any<CancellationToken>())
        .Returns(roleDto);

    using var host = await CreateBindingHostAsync(mediator);
    using var client = host.GetTestClient();
    var request = new CreateRoleRequest("Custom Role", "Description", null);
    var json = JsonSerializer.Serialize(request, new JsonSerializerOptions(JsonSerializerDefaults.Web));
    using var response = await client.PostAsync("/api/v1/roles",
        new StringContent(json, Encoding.UTF8, "application/json"));

    Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    await mediator.Received(1).Send(Arg.Is<CreateRoleCommand>(c =>
        c.Name == "Custom Role" && c.Description == "Description"), Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task UpdateRolePermissions_DispatchesCommand_AndReturnsOk()
  {
    var roleId = Guid.NewGuid();
    var permId = Guid.NewGuid();
    var mediator = Substitute.For<ISender>();
    var roleDetailDto = new RoleDetailDto(roleId, "Custom Role", null, false, null,
        [new(permId, Guid.NewGuid(), "roles", "read", "roles:read", "Read roles")],
        DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);

    mediator.Send(Arg.Any<UpdateRolePermissionsCommand>(), Arg.Any<CancellationToken>())
        .Returns(roleDetailDto);

    using var host = await CreateBindingHostAsync(mediator);
    using var client = host.GetTestClient();
    var request = new UpdateRolePermissionsRequest([permId]);
    var json = JsonSerializer.Serialize(request, new JsonSerializerOptions(JsonSerializerDefaults.Web));
    using var response = await client.PutAsync($"/api/v1/roles/{roleId}/permissions",
        new StringContent(json, Encoding.UTF8, "application/json"));

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    await mediator.Received(1).Send(Arg.Is<UpdateRolePermissionsCommand>(c =>
        c.RoleId == roleId && c.PermissionIds.Contains(permId)), Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task GetResources_DispatchesQuery_AndReturnsOk()
  {
    var mediator = Substitute.For<ISender>();
    var resources = new List<ResourceDto>
        {
            new(Guid.NewGuid(), "roles", "Role Management", [])
        };
    mediator.Send(Arg.Any<GetResourcesQuery>(), Arg.Any<CancellationToken>())
        .Returns(resources);

    using var host = await CreateBindingHostAsync(mediator);
    using var client = host.GetTestClient();
    using var response = await client.GetAsync("/api/v1/resources");

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    await mediator.Received(1).Send(Arg.Any<GetResourcesQuery>(), Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task GetPermissions_DispatchesQuery_AndReturnsOk()
  {
    var mediator = Substitute.For<ISender>();
    var permissions = new List<PermissionDto>
        {
            new(Guid.NewGuid(), Guid.NewGuid(), "roles", "read", "roles:read", "Read roles")
        };
    mediator.Send(Arg.Any<GetPermissionsQuery>(), Arg.Any<CancellationToken>())
        .Returns(permissions);

    using var host = await CreateBindingHostAsync(mediator);
    using var client = host.GetTestClient();
    using var response = await client.GetAsync("/api/v1/permissions");

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    await mediator.Received(1).Send(Arg.Any<GetPermissionsQuery>(), Arg.Any<CancellationToken>());
  }
}
