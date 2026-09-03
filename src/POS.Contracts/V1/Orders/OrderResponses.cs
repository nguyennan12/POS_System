namespace POS.Contracts.V1.Orders;

public record OrderItemResponse(
    Guid Id,
    Guid SkuId,
    string SkuCode,
    string ProductName,
    decimal Qty,
    decimal UnitPrice,
    decimal DiscountAmount,
    decimal TaxAmount,
    decimal LineTotal
);

public record OrderDiscountResponse(
    Guid Id,
    Guid? PromotionId,
    Guid? VoucherId,
    decimal DiscountAmount,
    string? Description,
    DateTimeOffset AppliedAt
);

public record OrderPaymentResponse(
    Guid Id,
    string Method,
    decimal Amount,
    decimal? ChangeAmount,
    string? TransactionRef,
    string Status,
    DateTimeOffset? PaidAt
);

public record OrderSummaryResponse(
    Guid Id,
    Guid StoreId,
    Guid ShiftId,
    Guid? CustomerId,
    string? CustomerName,
    string? CustomerPhone,
    string Status,
    string CurrencyCode,
    decimal GrandTotal,
    int TotalItems,
    DateTimeOffset CreatedAt,
    DateTimeOffset? PaidAt
);

public record OrderDetailResponse(
    Guid Id,
    Guid StoreId,
    Guid ShiftId,
    Guid? CustomerId,
    string? CustomerName,
    string? CustomerPhone,
    string Status,
    string CurrencyCode,
    decimal Subtotal,
    decimal DiscountTotal,
    decimal TaxTotal,
    decimal GrandTotal,
    string? Note,
    Guid CreatedBy,
    DateTimeOffset CreatedAt,
    DateTimeOffset? PaidAt,
    IReadOnlyList<OrderItemResponse> Items,
    IReadOnlyList<OrderDiscountResponse> Discounts,
    IReadOnlyList<OrderPaymentResponse> Payments
);

public record ReceiptDataResponse(
    string StoreName,
    string? StoreAddress,
    string? StorePhone,
    string OrderNo,
    string CashierName,
    DateTimeOffset CreatedAt,
    IReadOnlyList<OrderItemResponse> Items,
    decimal Subtotal,
    decimal DiscountTotal,
    decimal TaxTotal,
    decimal GrandTotal,
    decimal AmountPaid,
    decimal ChangeAmount,
    string? ReceiptHeader,
    string? ReceiptFooter
);

public record CheckoutResponse(
    Guid OrderId,
    decimal GrandTotal,
    decimal TotalPaid,
    decimal ChangeAmount,
    string Status,
    IReadOnlyList<OrderPaymentResponse> Payments,
    ReceiptDataResponse? ReceiptData = null
);
