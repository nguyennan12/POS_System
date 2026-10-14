using POS.Domain.Common;

namespace POS.Domain.Orders.Events;

public sealed record OrderPaidDomainEvent(
    Guid OrderId,
    Guid StoreId,
    Guid ShiftId,
    Guid CreatedBy,
    Guid? CustomerId,
    decimal GrandTotal,
    DateTime PaidAt,
    IReadOnlyList<OrderItemPaidDetail> Items
) : IDomainEvent;

public sealed record OrderItemPaidDetail(
    Guid SkuId,
    decimal Qty,
    decimal UnitPrice,
    decimal LineTotal
);
