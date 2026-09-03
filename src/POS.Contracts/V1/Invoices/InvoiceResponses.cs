namespace POS.Contracts.V1.Invoices;

public record InvoiceItemResponse(
    Guid SkuId,
    string ProductName,
    string SkuCode,
    decimal Qty,
    decimal UnitPrice,
    decimal TotalPrice
);

public record InvoiceSummaryResponse(
    Guid Id,
    Guid OrderId,
    string InvoiceNo,
    string? BuyerName,
    decimal TotalBeforeTax,
    decimal TaxAmount,
    decimal GrandTotal,
    DateTimeOffset IssuedAt
);

public record InvoiceDetailResponse(
    Guid Id,
    Guid OrderId,
    string InvoiceNo,
    string? BuyerName,
    string? BuyerTaxCode,
    string? BuyerAddress,
    decimal TotalBeforeTax,
    decimal TaxAmount,
    decimal GrandTotal,
    DateTimeOffset IssuedAt,
    IReadOnlyList<InvoiceItemResponse> Items
);

public record InvoicePdfResponse(
    Guid InvoiceId,
    string InvoiceNo,
    string PdfUrl
);
