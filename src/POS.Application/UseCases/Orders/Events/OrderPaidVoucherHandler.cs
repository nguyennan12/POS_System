using MediatR;
using Microsoft.Extensions.Logging;
using POS.Application.Abstractions.Persistence;
using POS.Domain.Orders.Enums;
using POS.Domain.Orders.Events;
using POS.Domain.Promotions;

namespace POS.Application.UseCases.Orders.Events;

/// <summary>
/// Handles OrderPaidDomainEvent to record voucher usage on paid order (Task T53).
/// </summary>
public sealed class OrderPaidVoucherHandler(
    IOrderRepository orderRepository,
    IVoucherRepository voucherRepository,
    IVoucherUsageRepository voucherUsageRepository,
    ILogger<OrderPaidVoucherHandler> logger) : INotificationHandler<OrderPaidDomainEvent>
{
    public async Task Handle(OrderPaidDomainEvent notification, CancellationToken cancellationToken)
    {
        var order = await orderRepository.GetByIdWithDetailsAsync(notification.OrderId, cancellationToken);
        if (order is null || order.Status != OrderStatus.Paid)
            return;

        if (!order.AppliedVoucherId.HasValue)
            return;

        var voucher = await voucherRepository.GetByIdWithPromotionAsync(order.AppliedVoucherId.Value, cancellationToken);
        if (voucher is null)
        {
            logger.LogWarning("Voucher {VoucherId} not found when processing paid order {OrderId}",
                order.AppliedVoucherId.Value, order.Id);
            return;
        }

        voucher.RecordUse();

        if (order.CustomerId.HasValue)
        {
            var usage = VoucherUsage.Create(
                voucherId: voucher.Id,
                customerId: order.CustomerId.Value,
                orderId: order.Id);
            await voucherUsageRepository.AddAsync(usage, cancellationToken);
        }
    }
}
