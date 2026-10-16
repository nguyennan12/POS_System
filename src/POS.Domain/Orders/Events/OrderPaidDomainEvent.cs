using POS.Domain.Common;

namespace POS.Domain.Orders.Events;

public sealed record OrderPaidDomainEvent(
    Guid OrderId,
    Guid StoreId = default,
    Guid ShiftId = default,
    Guid CreatedBy = default,
    Guid? CustomerId = null,
    decimal GrandTotal = 0,
    DateTime PaidAt = default,
    IReadOnlyList<OrderItemPaidDetail>? Items = null
) : IDomainEvent
{
    public OrderPaidDomainEvent(Guid orderId) : this(orderId, default, default, default, null, 0, default, null)
    {
    }
}

public sealed record OrderItemPaidDetail(
    Guid SkuId,
    decimal Qty,
    decimal UnitPrice,
    decimal LineTotal
);
