namespace POS.WinUI.Core.Models.Cfd;

public record CfdCartItemDto(
    Guid ProductId,
    string Sku,
    string Name,
    string Unit,
    decimal Price,
    int Quantity,
    decimal TotalAmount,
    string? ImageUrl = null
);

public record CfdCartStateDto(
    IReadOnlyList<CfdCartItemDto> Items,
    int TotalItems,
    decimal SubTotal,
    decimal DiscountAmount,
    decimal TaxAmount,
    decimal GrandTotal,
    string? CustomerName = null,
    string? CustomerPhone = null,
    decimal PointsEarned = 0,
    string? CustomerTier = null,
    decimal CustomerPointsBalance = 0
);

public record CfdPaymentQrDto(
    decimal Amount,
    string QrDataUrl,
    string OrderCode,
    string BankName,
    string AccountNumber,
    string AccountName
);
