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
    decimal CustomerPointsBalance = 0,
    decimal VoucherDiscount = 0,
    decimal PointsDiscount = 0,
    decimal PointsUsed = 0
);

public record CfdPaymentQrDto(
    decimal Amount,
    string QrDataUrl,
    string OrderCode,
    string BankName,
    string AccountNumber,
    string AccountName
);

public record CfdSplitPaymentItemDto(
    string Method,
    string Title,
    decimal Amount,
    string PercentageText,
    bool IsCash = false,
    decimal TenderedCash = 0,
    decimal ChangeAmount = 0,
    string? ImagePath = null,
    string? BadgeBg = null,
    string? BadgeFg = null
);

public record CfdPaymentStateDto(
    string PaymentMethod,
    decimal GrandTotal,
    decimal TenderedCash,
    decimal ChangeAmount,
    string? QrDataUrl = null,
    string? OrderCode = null,
    string? BankName = null,
    string? AccountNumber = null,
    string? AccountName = null,
    string? TransferContent = null,
    string? StatusText = null,
    bool IsCompleted = false,
    decimal PointsUsed = 0,
    decimal PointsDiscount = 0,
    decimal VoucherDiscount = 0,
    decimal SubTotal = 0,
    decimal PointsBalanceRemaining = 0,
    decimal PointsEarned = 0,
    string? CustomerName = null,
    string? CustomerPhone = null,
    string? CustomerTier = null,
    bool IsSplitPayment = false,
    IReadOnlyList<CfdSplitPaymentItemDto>? SplitItems = null,
    string? ActiveSplitQrMethod = null,
    decimal SplitQrAmount = 0
);
