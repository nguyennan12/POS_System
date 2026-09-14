using System.Reflection;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using POS.Api.Auth;
using POS.Api.Controllers;
using POS.Api.Extensions;
using POS.Api.Mappings;
using POS.Application.UseCases.Stores.Queries.GetStoreDetail;
using POS.Domain.Common;
using POS.Domain.Stores;
using System.Net;
using System.Text;
using System.Text.Json;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NSubstitute;
using POS.Application.UseCases.Stores.Commands.UpdateStoreStatus;
using POS.Contracts.V1.Stores;

namespace POS.Api.Tests.Stores;

public class StoresControllerTests
{
    [Theory]
    [InlineData("{}")]
    [InlineData("{\"active\":false}")]
    [InlineData("{\"isActve\":true}")]
    [InlineData("{\"isActive\":null}")]
    [InlineData("{\"isActive\":\"false\"}")]
    [InlineData("{\"isActive\":0}")]
    public async Task Status_json_missing_or_invalid_field_returns_400_without_dispatch(string json)
    {
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<UpdateStoreStatusRequest>(
            json, new JsonSerializerOptions(JsonSerializerDefaults.Web)));

        var mediator = Substitute.For<ISender>();
        using var host = await CreateBindingHostAsync(mediator);
        using var client = host.GetTestClient();
        using var response = await client.PutAsync($"/api/v1/stores/{Guid.NewGuid()}/status",
            new StringContent(json, Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Empty(mediator.ReceivedCalls());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Status_json_preserves_explicit_boolean_and_dispatches(bool isActive)
    {
        var store = new Store("Store", isActive: isActive);
        var json = JsonSerializer.Serialize(new UpdateStoreStatusRequest(isActive),
            new JsonSerializerOptions(JsonSerializerDefaults.Web));
        var bound = JsonSerializer.Deserialize<UpdateStoreStatusRequest>(json,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.Equal(isActive, bound!.IsActive);

        var mediator = Substitute.For<ISender>();
        mediator.Send(Arg.Any<UpdateStoreStatusCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result<StoreDetailDto>.Success(StoreDetailDto.FromStore(store)));
        using var host = await CreateBindingHostAsync(mediator);
        using var client = host.GetTestClient();
        using var response = await client.PutAsync($"/api/v1/stores/{store.Id}/status",
            new StringContent(json, Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await mediator.Received(1).Send(Arg.Is<UpdateStoreStatusCommand>(c =>
            c.StoreId == store.Id && c.IsActive == isActive), Arg.Any<CancellationToken>());
    }

    // Runs the real MVC input formatter/model binding and StoresController. Identity is
    // supplied by this test host only; JWT validation and handler behavior are separate tests.
    private static Task<IHost> CreateBindingHostAsync(ISender mediator) => new HostBuilder()
        .ConfigureWebHost(builder => builder.UseTestServer()
        .ConfigureServices(services =>
        {
            services.AddSingleton(mediator);
            services.AddAuthentication();
            services.AddAuthorization();
            services.AddControllers().AddApplicationPart(typeof(StoresController).Assembly);
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

    [Theory]
    [InlineData(nameof(StoresController.GetAll), "GET", null)]
    [InlineData(nameof(StoresController.GetById), "GET", "{id:guid}")]
    [InlineData(nameof(StoresController.Create), "POST", null)]
    [InlineData(nameof(StoresController.Update), "PUT", "{id:guid}")]
    [InlineData(nameof(StoresController.UpdateStatus), "PUT", "{id:guid}/status")]
    [InlineData(nameof(StoresController.AssignAdmin), "POST", "{id:guid}/assign-admin")]
    [InlineData(nameof(StoresController.GrantOwnerAccess), "POST", "{id:guid}/grant-owner-access")]
    public void Store_routes_keep_contract_paths_and_require_authentication(string action, string verb, string? path)
    {
        Assert.NotNull(typeof(StoresController).GetCustomAttribute<AuthorizeAttribute>());
        Assert.Equal("api/v1/stores", typeof(StoresController).GetCustomAttribute<RouteAttribute>()!.Template);
        var route = typeof(StoresController).GetMethod(action)!.GetCustomAttribute<HttpMethodAttribute>()!;
        Assert.Contains(verb, route.HttpMethods);
        Assert.Equal(path, route.Template);
    }

    [Theory]
    [InlineData(ErrorType.Unauthorized, 401)]
    [InlineData(ErrorType.Forbidden, 403)]
    [InlineData(ErrorType.NotFound, 404)]
    [InlineData(ErrorType.AlreadyExists, 409)]
    public void Result_errors_map_to_expected_http_status(ErrorType type, int expected)
    {
        var controller = new StoresController(null!);
        var result = controller.ToActionResult(Result<object>.Failure(new Error(type, "Test")));
        Assert.Equal(expected, Assert.IsAssignableFrom<ObjectResult>(result).StatusCode);
    }

    [Fact]
    public void Current_user_reads_employee_id_only_from_authenticated_principal()
    {
        var id = Guid.NewGuid();
        var context = new DefaultHttpContext();
        var accessor = new HttpContextAccessor { HttpContext = context };
        var user = new CurrentUser(accessor);
        context.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("employee_id", id.ToString())]));
        Assert.Null(user.EmployeeId);
        context.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("employee_id", id.ToString())], "Bearer"));
        Assert.Equal(id, user.EmployeeId);
    }

    [Fact]
    public void Detail_mapping_preserves_persisted_settings()
    {
        var store = new Store("Store", phone: "0123", timezone: "UTC", currencyCode: "USD",
            taxCode: "Tax", receiptHeader: "Header", receiptFooter: "Footer");
        var response = StoreDetailDto.FromStore(store).ToResponse();
        Assert.Equal(store.Phone, response.Phone);
        Assert.Equal(store.Timezone, response.Timezone);
        Assert.Equal(store.CurrencyCode, response.CurrencyCode);
        Assert.Equal(store.TaxCode, response.TaxCode);
        Assert.Equal(store.ReceiptHeader, response.ReceiptHeader);
        Assert.Equal(store.ReceiptFooter, response.ReceiptFooter);
        Assert.Equal(new DateTimeOffset(store.CreatedAt), response.CreatedAt);
    }
}
