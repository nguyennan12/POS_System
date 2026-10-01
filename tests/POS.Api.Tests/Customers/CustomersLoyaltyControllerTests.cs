using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using System.Security.Claims;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NSubstitute;
using POS.Api.Controllers;
using POS.Application.UseCases.Customers;
using POS.Application.UseCases.Customers.Commands.AccruePoints;
using POS.Application.UseCases.Customers.Commands.AdjustPoints;
using POS.Application.UseCases.Customers.Commands.RedeemPoints;
using POS.Application.UseCases.Customers.Queries.GetLoyaltyAccount;
using POS.Application.UseCases.Customers.Queries.GetPointTransactions;
using POS.Contracts.V1.Common;
using POS.Contracts.V1.Customers;
using POS.Domain.Common;
using POS.Domain.Customers.Errors;
using Xunit;

namespace POS.Api.Tests.Customers;

public class CustomersLoyaltyControllerTests
{
    private static Task<IHost> CreateBindingHostAsync(ISender mediator) => new HostBuilder()
        .ConfigureWebHost(builder => builder.UseTestServer()
            .ConfigureServices(services =>
            {
                services.AddSingleton(mediator);
                services.AddAuthentication();
                services.AddAuthorization();
                services.AddControllers().AddApplicationPart(typeof(CustomersController).Assembly);
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
    public void Customers_controller_has_authorize_and_correct_route()
    {
        typeof(CustomersController).GetCustomAttribute<AuthorizeAttribute>().Should().NotBeNull();
        typeof(CustomersController).GetCustomAttribute<RouteAttribute>()!.Template.Should().Be("api/v1/customers");
    }

    [Fact]
    public async Task GetLoyaltyAccount_DispatchesQuery_AndReturnsOk()
    {
        var customerId = Guid.NewGuid();
        var mediator = Substitute.For<ISender>();
        var dto = new LoyaltyAccountDto(customerId, 150, "Gold", 0.02m, 0.05m, DateTimeOffset.UtcNow);

        mediator.Send(Arg.Is<GetLoyaltyAccountQuery>(q => q.CustomerId == customerId), Arg.Any<CancellationToken>())
            .Returns(Result<LoyaltyAccountDto>.Success(dto));

        using var host = await CreateBindingHostAsync(mediator);
        var client = host.GetTestClient();

        var response = await client.GetAsync($"/api/v1/customers/{customerId}/loyalty");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadFromJsonAsync<ApiResponse<LoyaltyAccountResponse>>();
        content.Should().NotBeNull();
        content!.Success.Should().BeTrue();
        content.Data!.CustomerId.Should().Be(customerId);
        content.Data.PointsBalance.Should().Be(150);
        content.Data.TierName.Should().Be("Gold");
    }

    [Fact]
    public async Task GetPointTransactions_DispatchesQuery_AndReturnsPagedResponse()
    {
        var customerId = Guid.NewGuid();
        var mediator = Substitute.For<ISender>();
        var pagedList = new PagedPointTransactionList(
            [
                new PointTransactionDto(Guid.NewGuid(), customerId, 50, "Earn", null, "Note 1", DateTimeOffset.UtcNow),
                new PointTransactionDto(Guid.NewGuid(), customerId, 20, "Redeem", null, "Note 2", DateTimeOffset.UtcNow)
            ],
            2,
            1,
            20
        );

        mediator.Send(Arg.Is<GetPointTransactionsQuery>(q => q.CustomerId == customerId), Arg.Any<CancellationToken>())
            .Returns(Result<PagedPointTransactionList>.Success(pagedList));

        using var host = await CreateBindingHostAsync(mediator);
        var client = host.GetTestClient();

        var response = await client.GetAsync($"/api/v1/customers/{customerId}/loyalty/transactions?pageNumber=1&pageSize=20");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadFromJsonAsync<ApiResponse<PagedResponse<PointTransactionResponse>>>();
        content.Should().NotBeNull();
        content!.Success.Should().BeTrue();
        content.Data!.Items.Should().HaveCount(2);
        content.Data.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task AccruePoints_DispatchesCommand_AndReturnsOk()
    {
        var customerId = Guid.NewGuid();
        var mediator = Substitute.For<ISender>();
        var dto = new LoyaltyAccountDto(customerId, 200, "Silver", 0.015m, 0.02m, DateTimeOffset.UtcNow);

        mediator.Send(Arg.Is<AccruePointsCommand>(c => c.CustomerId == customerId && c.Points == 50), Arg.Any<CancellationToken>())
            .Returns(Result<LoyaltyAccountDto>.Success(dto));

        using var host = await CreateBindingHostAsync(mediator);
        var client = host.GetTestClient();

        var response = await client.PostAsJsonAsync($"/api/v1/customers/{customerId}/loyalty/accrue", new AccruePointsRequest(50, null, "Mua hàng"));
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadFromJsonAsync<ApiResponse<LoyaltyAccountResponse>>();
        content.Should().NotBeNull();
        content!.Data!.PointsBalance.Should().Be(200);
    }

    [Fact]
    public async Task RedeemPoints_DispatchesCommand_AndReturnsOk()
    {
        var customerId = Guid.NewGuid();
        var mediator = Substitute.For<ISender>();
        var dto = new LoyaltyAccountDto(customerId, 80, "Silver", 0.015m, 0.02m, DateTimeOffset.UtcNow);

        mediator.Send(Arg.Is<RedeemPointsCommand>(c => c.CustomerId == customerId && c.Points == 20), Arg.Any<CancellationToken>())
            .Returns(Result<LoyaltyAccountDto>.Success(dto));

        using var host = await CreateBindingHostAsync(mediator);
        var client = host.GetTestClient();

        var response = await client.PostAsJsonAsync($"/api/v1/customers/{customerId}/loyalty/redeem", new RedeemPointsRequest(20, null, "Đổi điểm"));
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadFromJsonAsync<ApiResponse<LoyaltyAccountResponse>>();
        content.Should().NotBeNull();
        content!.Data!.PointsBalance.Should().Be(80);
    }

    [Fact]
    public async Task AdjustPoints_DispatchesCommand_AndReturnsOk()
    {
        var customerId = Guid.NewGuid();
        var mediator = Substitute.For<ISender>();
        var dto = new LoyaltyAccountDto(customerId, 110, "Silver", 0.015m, 0.02m, DateTimeOffset.UtcNow);

        mediator.Send(Arg.Is<AdjustPointsCommand>(c => c.CustomerId == customerId && c.Points == 10 && c.Note == "Điều chỉnh điểm"), Arg.Any<CancellationToken>())
            .Returns(Result<LoyaltyAccountDto>.Success(dto));

        using var host = await CreateBindingHostAsync(mediator);
        var client = host.GetTestClient();

        var response = await client.PostAsJsonAsync($"/api/v1/customers/{customerId}/loyalty/adjust", new AdjustPointsRequest(10, "Điều chỉnh điểm"));
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadFromJsonAsync<ApiResponse<LoyaltyAccountResponse>>();
        content.Should().NotBeNull();
        content!.Data!.PointsBalance.Should().Be(110);
    }

    [Fact]
    public async Task RedeemPoints_InsufficientPoints_Returns400()
    {
        var customerId = Guid.NewGuid();
        var mediator = Substitute.For<ISender>();

        mediator.Send(Arg.Is<RedeemPointsCommand>(c => c.CustomerId == customerId), Arg.Any<CancellationToken>())
            .Returns(Result<LoyaltyAccountDto>.Failure(CustomerErrors.InsufficientPoints));

        using var host = await CreateBindingHostAsync(mediator);
        var client = host.GetTestClient();

        var response = await client.PostAsJsonAsync($"/api/v1/customers/{customerId}/loyalty/redeem", new RedeemPointsRequest(500));
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var content = await response.Content.ReadFromJsonAsync<ApiResponse<LoyaltyAccountResponse>>();
        content.Should().NotBeNull();
        content!.Success.Should().BeFalse();
        content.Error!.Code.Should().Be(CustomerErrors.InsufficientPoints.Code);
    }
}
