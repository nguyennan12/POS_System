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
using POS.Application.UseCases.Promotions;
using POS.Application.UseCases.Promotions.Commands.CreatePromotion;
using POS.Application.UseCases.Promotions.Commands.DeletePromotion;
using POS.Application.UseCases.Promotions.Commands.UpdatePromotion;
using POS.Application.UseCases.Promotions.Queries.GetPromotionById;
using POS.Application.UseCases.Promotions.Queries.GetPromotions;
using POS.Contracts.V1.Common;
using POS.Contracts.V1.Promotions;
using POS.Domain.Common;
using POS.Domain.Promotions.Errors;
using Xunit;

namespace POS.Api.Tests.Promotions;

public class PromotionsControllerTests
{
    private static Task<IHost> CreateBindingHostAsync(ISender mediator) => new HostBuilder()
        .ConfigureWebHost(builder => builder.UseTestServer()
            .ConfigureServices(services =>
            {
                services.AddHttpContextAccessor();
                services.AddScoped<POS.Application.Abstractions.Auth.ICurrentUser, POS.Api.Auth.CurrentUser>();
                services.AddSingleton(mediator);
                services.AddAuthentication();
                services.AddAuthorization();
                services.AddControllers().AddApplicationPart(typeof(PromotionsController).Assembly);
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
    public void Promotions_controller_has_authorize_and_correct_route()
    {
        typeof(PromotionsController).GetCustomAttribute<AuthorizeAttribute>().Should().NotBeNull();
        typeof(PromotionsController).GetCustomAttribute<RouteAttribute>()!.Template.Should().Be("api/v1/promotions");
    }

    [Fact]
    public async Task GetPromotions_DispatchesQuery_AndReturnsPagedResponse()
    {
        var mediator = Substitute.For<ISender>();
        var pagedList = new PagedPromotionList(
            [
                new PromotionSummaryDto(Guid.NewGuid(), Guid.Empty, "Promo 1", "CartFixed", 10000, "All", DateTimeOffset.UtcNow, null, "Active", DateTimeOffset.UtcNow)
            ],
            1,
            1,
            20
        );

        mediator.Send(Arg.Any<GetPromotionsQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result<PagedPromotionList>.Success(pagedList));

        using var host = await CreateBindingHostAsync(mediator);
        var client = host.GetTestClient();

        var response = await client.GetAsync("/api/v1/promotions?pageNumber=1&pageSize=20");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadFromJsonAsync<ApiResponse<PagedResponse<PromotionSummaryResponse>>>();
        content.Should().NotBeNull();
        content!.Success.Should().BeTrue();
        content.Data!.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetPromotionById_DispatchesQuery_AndReturnsOk()
    {
        var promoId = Guid.NewGuid();
        var mediator = Substitute.For<ISender>();
        var dto = new PromotionDetailDto(
            promoId, Guid.Empty, "Flash Sale", "CartFixed", 50000, 200000, null, null, 0, false, false, "All", DateTimeOffset.UtcNow, null, "Active", [], [], DateTimeOffset.UtcNow);

        mediator.Send(Arg.Is<GetPromotionByIdQuery>(q => q.Id == promoId), Arg.Any<CancellationToken>())
            .Returns(Result<PromotionDetailDto>.Success(dto));

        using var host = await CreateBindingHostAsync(mediator);
        var client = host.GetTestClient();

        var response = await client.GetAsync($"/api/v1/promotions/{promoId}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadFromJsonAsync<ApiResponse<PromotionDetailResponse>>();
        content.Should().NotBeNull();
        content!.Success.Should().BeTrue();
        content.Data!.Id.Should().Be(promoId);
    }

    [Fact]
    public async Task CreatePromotion_DispatchesCommand_AndReturnsCreated()
    {
        var mediator = Substitute.For<ISender>();
        var newId = Guid.NewGuid();
        var dto = new PromotionDetailDto(
            newId, Guid.Empty, "New Promo", "CartPercent", 10, 0, null, null, 0, false, false, "All", DateTimeOffset.UtcNow, null, "Active", [], [], DateTimeOffset.UtcNow);

        mediator.Send(Arg.Any<CreatePromotionCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result<PromotionDetailDto>.Success(dto));

        using var host = await CreateBindingHostAsync(mediator);
        var client = host.GetTestClient();

        var request = new CreatePromotionRequest("New Promo", "CartPercent", 10);
        var response = await client.PostAsJsonAsync("/api/v1/promotions", request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var content = await response.Content.ReadFromJsonAsync<ApiResponse<PromotionDetailResponse>>();
        content.Should().NotBeNull();
        content!.Data!.Id.Should().Be(newId);
    }

    [Fact]
    public async Task DeletePromotion_DispatchesCommand_AndReturnsOk()
    {
        var promoId = Guid.NewGuid();
        var mediator = Substitute.For<ISender>();

        mediator.Send(Arg.Is<DeletePromotionCommand>(c => c.Id == promoId), Arg.Any<CancellationToken>())
            .Returns(Result<bool>.Success(true));

        using var host = await CreateBindingHostAsync(mediator);
        var client = host.GetTestClient();

        var response = await client.DeleteAsync($"/api/v1/promotions/{promoId}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
