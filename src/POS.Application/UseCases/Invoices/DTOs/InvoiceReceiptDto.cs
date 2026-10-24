using POS.Application.UseCases.Orders.DTOs;

namespace POS.Application.UseCases.Invoices.DTOs;

public sealed record InvoiceReceiptDto(
    Guid InvoiceId,
    string InvoiceNo,
    DateTimeOffset IssuedAt,
    string? BuyerName,
    string? BuyerTaxCode,
    string? BuyerAddress,
    ReceiptDataDto Receipt,
    IReadOnlyList<OrderPaymentDto> Payments,
    bool IsReprint,
    int PaperWidth);

public sealed record InvoicePdfDto(Guid InvoiceId, string InvoiceNo, byte[]? FileBytes);
