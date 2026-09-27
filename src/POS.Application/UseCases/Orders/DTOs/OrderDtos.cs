namespace POS.Application.UseCases.Orders.DTOs;

public record OrderItemDto(
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

public record OrderDiscountDto(
    Guid Id,
    Guid? PromotionId,
    Guid? VoucherId,
    decimal DiscountAmount,
    string? Description,
    DateTimeOffset AppliedAt
);

public record OrderPaymentDto(
    Guid Id,
    string Method,
    decimal Amount,
    decimal? ChangeAmount,
    string? TransactionRef,
    string Status,
    DateTimeOffset? PaidAt
);

public record OrderDetailDto(
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
    IReadOnlyList<OrderItemDto> Items,
    IReadOnlyList<OrderDiscountDto> Discounts,
    IReadOnlyList<OrderPaymentDto> Payments
);

public record OrderSummaryDto(
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

public record ReceiptDataDto(
    string StoreName,
    string? StoreAddress,
    string? StorePhone,
    string OrderNo,
    string CashierName,
    DateTimeOffset CreatedAt,
    IReadOnlyList<OrderItemDto> Items,
    decimal Subtotal,
    decimal DiscountTotal,
    decimal TaxTotal,
    decimal GrandTotal,
    decimal AmountPaid,
    decimal ChangeAmount,
    string? ReceiptHeader,
    string? ReceiptFooter
);

public record CheckoutDto(
    Guid OrderId,
    decimal GrandTotal,
    decimal TotalPaid,
    decimal ChangeAmount,
    string Status,
    IReadOnlyList<OrderPaymentDto> Payments,
    ReceiptDataDto? ReceiptData = null
);
