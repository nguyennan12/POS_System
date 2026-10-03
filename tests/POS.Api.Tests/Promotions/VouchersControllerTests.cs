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
using POS.Application.UseCases.Promotions.Commands.DeleteVoucher;
using POS.Application.UseCases.Promotions.Commands.UpdateVoucher;
using POS.Application.UseCases.Promotions.Queries.GetVoucherById;
using POS.Application.UseCases.Promotions.Queries.GetVouchers;
using POS.Application.UseCases.Promotions.Queries.ValidateVoucher;
using POS.Contracts.V1.Common;
using POS.Contracts.V1.Promotions;
using POS.Domain.Common;
using Xunit;

namespace POS.Api.Tests.Promotions;

public class VouchersControllerTests
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
                services.AddControllers().AddApplicationPart(typeof(VouchersController).Assembly);
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
    public void Vouchers_controller_has_authorize_and_correct_route()
    {
        typeof(VouchersController).GetCustomAttribute<AuthorizeAttribute>().Should().NotBeNull();
        typeof(VouchersController).GetCustomAttribute<RouteAttribute>()!.Template.Should().Be("api/v1/vouchers");
    }

    [Fact]
    public async Task GetVouchers_DispatchesQuery_AndReturnsPagedResponse()
    {
        var mediator = Substitute.For<ISender>();
        var pagedList = new PagedVoucherList(
            [
                new VoucherDto(Guid.NewGuid(), Guid.NewGuid(), "Promo", "SUMMER2026", 100, 0, 1, null, true)
            ],
            1,
            1,
            20
        );

        mediator.Send(Arg.Any<GetVouchersQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result<PagedVoucherList>.Success(pagedList));

        using var host = await CreateBindingHostAsync(mediator);
        var client = host.GetTestClient();

        var response = await client.GetAsync("/api/v1/vouchers?pageNumber=1&pageSize=20");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadFromJsonAsync<ApiResponse<PagedResponse<VoucherResponse>>>();
        content.Should().NotBeNull();
        content!.Success.Should().BeTrue();
        content.Data!.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task ValidateVoucher_DispatchesQuery_AndReturnsValidationResult()
    {
        var mediator = Substitute.For<ISender>();
        var resultDto = new ValidateVoucherResultDto(
            IsValid: true,
            ErrorMessage: null,
            DiscountAmount: 20000,
            VoucherCode: "GIAM20",
            PromotionId: Guid.NewGuid(),
            PromotionName: "Giảm 20k"
        );

        mediator.Send(Arg.Is<ValidateVoucherQuery>(q => q.Code == "GIAM20" && q.OrderSubtotal == 100000), Arg.Any<CancellationToken>())
            .Returns(Result<ValidateVoucherResultDto>.Success(resultDto));

        using var host = await CreateBindingHostAsync(mediator);
        var client = host.GetTestClient();

        var response = await client.GetAsync("/api/v1/vouchers/GIAM20/validate?orderSubtotal=100000");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadFromJsonAsync<ApiResponse<ValidateVoucherResponse>>();
        content.Should().NotBeNull();
        content!.Success.Should().BeTrue();
        content.Data!.IsValid.Should().BeTrue();
        content.Data.DiscountAmount.Should().Be(20000);
        content.Data.VoucherCode.Should().Be("GIAM20");
    }
}
