using POS.Application.Abstractions.Payments;
using POS.Application.UseCases.Orders.Commands.CheckoutOrder;
using POS.Application.UseCases.Orders.Errors;
using POS.Domain.Common;
using POS.Domain.Orders;
using POS.Domain.Orders.Enums;

namespace POS.Application.UseCases.Payments.Strategies;

public class MoMoPaymentStrategy : IPaymentStrategy
{
    public PaymentMethod Method => PaymentMethod.MoMo;

    public Task<Result> ValidateAsync(PaymentSplitInputDto payment, Order order, CancellationToken cancellationToken = default)
    {
        if (payment.Amount <= 0)
            return Task.FromResult<Result>(OrderErrors.InvalidPaymentAmount);

        if (string.IsNullOrWhiteSpace(payment.TransactionRef))
            return Task.FromResult<Result>(OrderErrors.TransactionRefRequired);

        return Task.FromResult(Result.Success());
    }

    public Task ProcessPostPaidAsync(Payment payment, Order order, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
