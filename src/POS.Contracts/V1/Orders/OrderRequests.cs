using POS.Contracts.V1.Common;

namespace POS.Contracts.V1.Orders;

public record OrderFilterRequest(
    Guid? StoreId = null,
    Guid? ShiftId = null,
    string? Status = null,
    DateTimeOffset? From = null,
    DateTimeOffset? To = null,
    int PageNumber = 1,
    int PageSize = 20
) : PagedRequest(PageNumber, PageSize);

public record CreateOrderRequest(
    Guid ShiftId,
    Guid? CustomerId = null
);

public record AddOrderItemRequest(
    Guid SkuId,
    decimal Qty
);

public record ApplyVoucherRequest(
    string Code
);

public record PaymentSplitRequest(
    string Method,
    decimal Amount,
    string? TransactionRef = null
);

public record CheckoutOrderRequest(
    IReadOnlyList<PaymentSplitRequest> Payments,
    Guid? CustomerId = null
);

public record CancelOrderRequest(
    string? Reason = null
);
