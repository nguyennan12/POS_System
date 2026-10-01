using POS.Application.Abstractions.Payments;
using POS.Application.Abstractions.Persistence;
using POS.Application.UseCases.Orders.Commands.CheckoutOrder;
using POS.Application.UseCases.Orders.Errors;
using POS.Domain.Common;
using POS.Domain.Customers.Errors;
using POS.Domain.Orders;
using POS.Domain.Orders.Enums;

namespace POS.Application.UseCases.Payments.Strategies;

public class PointsPaymentStrategy(ICustomerRepository customerRepository) : IPaymentStrategy
{
    public PaymentMethod Method => PaymentMethod.Points;

    public async Task<Result> ValidateAsync(
        PaymentSplitInputDto payment, Order order, CancellationToken cancellationToken = default)
    {
        if (payment.Amount <= 0)
            return OrderErrors.InvalidPaymentAmount;

        if (!order.CustomerId.HasValue)
            return OrderErrors.PointsRequireCustomer;

        var loyalty = await customerRepository.GetByIdAsync(order.CustomerId.Value, cancellationToken);
        if (loyalty is null)
            return OrderErrors.CustomerNotFound;

        if (!loyalty.Customer.IsActive)
            return CustomerErrors.Inactive;

        if (loyalty.PointsBalance < payment.Amount)
            return OrderErrors.InsufficientPoints;

        return Result.Success();
    }

    public Task ProcessPostPaidAsync(Payment payment, Order order, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
