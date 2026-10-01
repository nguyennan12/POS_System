using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using POS.Api.Controllers;
using POS.Application;
using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Caching;
using POS.Application.Abstractions.Persistence;
using POS.Contracts.V1.Common;
using POS.Contracts.V1.Invoices;
using POS.Domain.Employees;
using POS.Domain.Orders;
using POS.Domain.Products;

namespace POS.Api.Tests.Invoices;

public class InvoicesControllerTests
{
    [Fact]
    public async Task GetInvoices_BindsDefaultsAndFilters_AndReturnsPagedEnvelope()
    {
        await using var fixture = await ApiFixture.CreateAsync();
        var summary = new InvoiceSummaryResponse(fixture.Invoice.Id, fixture.Invoice.OrderId,
            fixture.Invoice.InvoiceNo, "Buyer", 200, 18, 198, fixture.Invoice.IssuedAt);
        fixture.Invoices.GetPagedAsync(fixture.Employee.Id, fixture.Employee.StoreId, false,
            null, null, null, 1, 20, Arg.Any<CancellationToken>()).Returns((new List<InvoiceSummaryResponse> { summary }, 1));
        var response = await fixture.Client.GetAsync("/api/v1/invoices");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = (await response.Content.ReadFromJsonAsync<ApiResponse<PagedResponse<InvoiceSummaryResponse>>>())!;
        Assert.True(body.Success);
        Assert.Equal(1, body.Data!.PageNumber);
        Assert.Equal(20, body.Data.PageSize);
        Assert.Equal(1, body.Data.TotalCount);
        Assert.Equal(summary, Assert.Single(body.Data.Items));

        var from = new DateTimeOffset(2026, 9, 1, 7, 0, 0, TimeSpan.FromHours(7));
        var to = from.AddDays(1);
        fixture.Invoices.GetPagedAsync(fixture.Employee.Id, fixture.Employee.StoreId, false,
            fixture.Invoice.OrderId, from, to, 2, 1, Arg.Any<CancellationToken>())
            .Returns((new List<InvoiceSummaryResponse>(), 1));
        response = await fixture.Client.GetAsync($"/api/v1/invoices?orderId={fixture.Invoice.OrderId}" +
            $"&from={Uri.EscapeDataString(from.ToString("O"))}&to={Uri.EscapeDataString(to.ToString("O"))}&pageNumber=2&pageSize=1");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        body = (await response.Content.ReadFromJsonAsync<ApiResponse<PagedResponse<InvoiceSummaryResponse>>>())!;
        Assert.Empty(body.Data!.Items);
        Assert.Equal(2, body.Data.PageNumber);
        Assert.Equal(1, body.Data.PageSize);
        Assert.True(body.Data.HasPreviousPage);
        await fixture.Invoices.Received(1).GetPagedAsync(fixture.Employee.Id, fixture.Employee.StoreId, false,
            fixture.Invoice.OrderId, from, to, 2, 1, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetInvoiceById_ReturnsDetailAndItems_Or404ForUnknownId()
    {
        await using var fixture = await ApiFixture.CreateAsync();
        var response = await fixture.Client.GetAsync($"/api/v1/invoices/{fixture.Invoice.Id}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = (await response.Content.ReadFromJsonAsync<ApiResponse<InvoiceDetailResponse>>())!;
        Assert.True(body.Success);
        Assert.Equal(fixture.Invoice.Id, body.Data!.Id);
        Assert.Equal("TAX", body.Data.BuyerTaxCode);
        Assert.Equal("Address", body.Data.BuyerAddress);
        Assert.Equal(198, body.Data.GrandTotal);
        var item = Assert.Single(body.Data.Items);
        Assert.Equal("Product", item.ProductName);
        Assert.Equal("INVOICE", item.SkuCode);
        Assert.Equal(2, item.Qty);
        Assert.Equal(100, item.UnitPrice);
        Assert.Equal(198, item.TotalPrice);

        response = await fixture.Client.GetAsync($"/api/v1/invoices/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var failure = (await response.Content.ReadFromJsonAsync<ApiResponse<object>>())!;
        Assert.False(failure.Success);
        Assert.Equal("INVOICE.NOT_FOUND", failure.Error!.Code);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task BothRoutes_RequireAuthenticationAndOrdersReadPermission(bool detail)
    {
        await using var fixture = await ApiFixture.CreateAsync();
        var route = detail ? $"/api/v1/invoices/{fixture.Invoice.Id}" : "/api/v1/invoices";
        fixture.Client.DefaultRequestHeaders.Remove("X-Test-Auth");
        Assert.Equal(HttpStatusCode.Unauthorized, (await fixture.Client.GetAsync(route)).StatusCode);
        fixture.Client.DefaultRequestHeaders.Add("X-Test-Auth", "yes");
        fixture.Cache.GetAsync<string[]>($"perm:{fixture.Employee.Id}", Arg.Any<CancellationToken>())
            .Returns(["orders:update"]);
        Assert.Equal(HttpStatusCode.Forbidden, (await fixture.Client.GetAsync(route)).StatusCode);
        Assert.Empty(fixture.Invoices.ReceivedCalls());
    }

    [Theory]
    [InlineData("?pageNumber=0")]
    [InlineData("?pageSize=0")]
    [InlineData("?pageSize=101")]
    [InlineData("?from=2026-09-02T00:00:00Z&to=2026-09-01T00:00:00Z")]
    [InlineData("?orderId=00000000-0000-0000-0000-000000000000")]
    [InlineData("/00000000-0000-0000-0000-000000000000")]
    public async Task BothRoutes_Return400ForInvalidInput(string suffix)
    {
        await using var fixture = await ApiFixture.CreateAsync();
        var response = await fixture.Client.GetAsync("/api/v1/invoices" + suffix);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = (await response.Content.ReadFromJsonAsync<ApiResponse<object>>())!;
        Assert.False(body.Success);
        Assert.NotEmpty(body.Error!.ValidationErrors!);
        Assert.Empty(fixture.Invoices.ReceivedCalls());
    }

    [Fact]
    public async Task GetInvoiceById_Returns403ForInaccessibleStore()
    {
        await using var fixture = await ApiFixture.CreateAsync();
        var anotherOrder = Order.CreateDraft(Guid.NewGuid(), Guid.NewGuid(), fixture.Employee.Id);
        typeof(Invoice).GetProperty(nameof(Invoice.Order))!.SetValue(fixture.Invoice, anotherOrder);
        var response = await fixture.Client.GetAsync($"/api/v1/invoices/{fixture.Invoice.Id}");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var body = (await response.Content.ReadFromJsonAsync<ApiResponse<object>>())!;
        Assert.Equal("INVOICE.INVALID_STORE", body.Error!.Code);
    }

    private sealed class ApiFixture : IAsyncDisposable
    {
        private WebApplication app = null!;
        public HttpClient Client { get; private set; } = null!;
        public IInvoiceRepository Invoices { get; } = Substitute.For<IInvoiceRepository>();
        public ICacheService Cache { get; } = Substitute.For<ICacheService>();
        public Employee Employee { get; } = new("Employee", "employee", "hash", "hash", Guid.NewGuid(), storeId: Guid.NewGuid());
        public Invoice Invoice { get; private set; } = null!;

        public static async Task<ApiFixture> CreateAsync()
        {
            var fixture = new ApiFixture();
            var employee = fixture.Employee;
            var user = Substitute.For<ICurrentUser>();
            user.EmployeeId.Returns(employee.Id);
            user.IsAuthenticated.Returns(true);
            var employees = Substitute.For<IEmployeeRepository>();
            employees.GetByIdAsync(employee.Id, Arg.Any<CancellationToken>()).Returns(employee);
            fixture.Cache.GetAsync<string[]>($"perm:{employee.Id}", Arg.Any<CancellationToken>()).Returns(["orders:read"]);
            var order = Order.CreateDraft(employee.StoreId!.Value, Guid.NewGuid(), employee.Id);
            var product = new Product(order.StoreId, Guid.NewGuid(), "Product", "piece");
            var sku = new Sku(product.Id, order.StoreId, "INVOICE", "123", 100, 50, 10, true) { Product = product };
            order.AddOrUpdateItem(sku, 2).ApplyDiscountAndTax(20, 10);
            fixture.Invoice = Invoice.Create(order.Id, "HD-TEST-20260930-000001", 200, 18, 198, "Buyer", "TAX", "Address");
            typeof(Invoice).GetProperty(nameof(Invoice.Order))!.SetValue(fixture.Invoice, order);
            fixture.Invoices.GetByIdWithDetailsAsync(fixture.Invoice.Id, Arg.Any<CancellationToken>()).Returns(fixture.Invoice);

            var builder = WebApplication.CreateBuilder();
            builder.WebHost.UseTestServer();
            builder.Services.AddApplication();
            builder.Services.AddSingleton(user);
            builder.Services.AddSingleton(employees);
            builder.Services.AddSingleton(fixture.Invoices);
            builder.Services.AddSingleton(fixture.Cache);
            builder.Services.AddSingleton(Substitute.For<IPermissionRepository>());
            builder.Services.AddControllers().AddApplicationPart(typeof(InvoicesController).Assembly);
            builder.Services.AddAuthentication("Test")
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", _ => { });
            builder.Services.AddAuthorization();
            fixture.app = builder.Build();
            fixture.app.UseAuthentication();
            fixture.app.UseAuthorization();
            fixture.app.MapControllers();
            await fixture.app.StartAsync();
            fixture.Client = fixture.app.GetTestClient();
            fixture.Client.DefaultRequestHeaders.Add("X-Test-Auth", "yes");
            return fixture;
        }

        public async ValueTask DisposeAsync()
        {
            Client.Dispose();
            await app.DisposeAsync();
        }
    }

    private sealed class TestAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger, UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync() =>
            Request.Headers["X-Test-Auth"] != "yes"
                ? Task.FromResult(AuthenticateResult.NoResult())
                : Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(
                    new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "test")], "Test")), "Test")));
    }
}
