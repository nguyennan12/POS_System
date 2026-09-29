using MediatR;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using POS.Api.Controllers;
using POS.Application.UseCases.Orders.DTOs;
using POS.Application.UseCases.Orders.Errors;
using POS.Application.UseCases.Orders.Mappings;
using POS.Application.UseCases.Orders.Queries.GetOrderById;
using POS.Contracts.V1.Common;
using POS.Contracts.V1.Payments;
using POS.Domain.Common;
using POS.Domain.Orders;
using POS.Domain.Orders.Enums;

namespace POS.Api.Tests.Orders;

public class PaymentStatusControllerTests
{
    [Theory]
    [InlineData(PaymentStatus.Success)]
    [InlineData(PaymentStatus.Pending)]
    [InlineData(PaymentStatus.Failed)]
    [InlineData(PaymentStatus.Timeout)]
    public async Task GetPaymentStatus_ShouldReturnActualRecordState(PaymentStatus status)
    {
        var order = Order.CreateDraft(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        var payment = new Payment(order.Id, PaymentMethod.Card, 40, "ref", status);
        order.Payments.Add(payment);
        var sender = Substitute.For<ISender>();
        sender.Send(Arg.Is<GetOrderByIdQuery>(q => q.Id == order.Id), Arg.Any<CancellationToken>())
            .Returns(Result<OrderDetailDto>.Success(order.ToDetailDto()));

        var result = await new OrdersController(sender).GetPaymentStatus(order.Id, payment.Id, default);
        var response = Assert.IsType<ApiResponse<PaymentStatusResponse>>(Assert.IsType<OkObjectResult>(result.Result).Value);
        Assert.Equal(payment.Id, response.Data!.PaymentId);
        Assert.Equal(order.Id, response.Data.OrderId);
        Assert.Equal(status.ToString(), response.Data.Status);
        Assert.Equal(40, response.Data.Amount);
        Assert.Equal("ref", response.Data.TransactionRef);
    }

    [Fact]
    public async Task GetPaymentStatus_ShouldRejectPaymentOutsideRequestedOrder()
    {
        var order = Order.CreateDraft(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        var sender = Substitute.For<ISender>();
        sender.Send(Arg.Any<GetOrderByIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result<OrderDetailDto>.Success(order.ToDetailDto()));
        var result = await new OrdersController(sender).GetPaymentStatus(order.Id, Guid.NewGuid(), default);
        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetPaymentStatus_ShouldPreserveOrderAccessFailure()
    {
        var sender = Substitute.For<ISender>();
        sender.Send(Arg.Any<GetOrderByIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result<OrderDetailDto>.Failure(OrderErrors.InvalidStore));
        var result = await new OrdersController(sender).GetPaymentStatus(Guid.NewGuid(), Guid.NewGuid(), default);
        Assert.Equal(403, Assert.IsType<ObjectResult>(result.Result).StatusCode);
    }
}
