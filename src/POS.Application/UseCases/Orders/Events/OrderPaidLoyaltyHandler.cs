using MediatR;
using POS.Application.Abstractions.Persistence;
using POS.Domain.Customers;
using POS.Domain.Customers.Enums;
using POS.Domain.Orders.Enums;
using POS.Domain.Orders.Events;

namespace POS.Application.UseCases.Orders.Events;

/// <summary>
/// Handles OrderPaidDomainEvent to accrue loyalty points and record cumulative spending for the customer (Task T37).
/// </summary>
public sealed class OrderPaidLoyaltyHandler(
    IOrderRepository orderRepository,
    ICustomerRepository customerRepository,
    IMemberTierRepository memberTierRepository) : INotificationHandler<OrderPaidDomainEvent>
{
    public async Task Handle(OrderPaidDomainEvent notification, CancellationToken cancellationToken)
    {
        var order = await orderRepository.GetByIdWithDetailsAsync(notification.OrderId, cancellationToken);
        if (order is null || order.Status != OrderStatus.Paid)
            return;

        if (!order.CustomerId.HasValue)
            return;

        var customer = await customerRepository.GetEntityByIdAsync(order.CustomerId.Value, cancellationToken);
        if (customer is null || !customer.IsActive)
            return;

        var availableTiers = await memberTierRepository.GetAllAsync(cancellationToken);
        customer.RecordSpending(order.GrandTotal, availableTiers);

        var tier = customer.MemberTier ?? availableTiers.FirstOrDefault(t => t.Id == customer.MemberTierId);
        var rate = tier?.PointRate ?? 0.01m;
        var pointsEarned = Math.Round(order.GrandTotal * rate, 0, MidpointRounding.AwayFromZero);

        if (pointsEarned <= 0)
            return;

        var loyaltyAccount = await customerRepository.GetLoyaltyAccountWithTierAsync(customer.Id, cancellationToken);
        if (loyaltyAccount is null)
        {
            loyaltyAccount = new LoyaltyAccount(customer.Id, 0);
            await customerRepository.AddLoyaltyAccountAsync(loyaltyAccount, cancellationToken);
        }

        loyaltyAccount.AddPoints(pointsEarned);

        var pointTx = new PointTransaction(
            customerId: customer.Id,
            points: pointsEarned,
            type: PointTransactionType.Earn,
            orderId: order.Id,
            note: $"Tích điểm từ đơn hàng #{order.Id}"
        );
        await customerRepository.AddPointTransactionAsync(pointTx, cancellationToken);
    }
}
