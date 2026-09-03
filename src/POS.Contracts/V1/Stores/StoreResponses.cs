namespace POS.Contracts.V1.Stores;

public record StoreResponse(
    Guid Id,
    string Name,
    string? Address,
    string? Phone,
    bool IsActive
);


public record StoreDetailResponse(
    Guid Id,
    string Name,
    string? Address,
    bool IsActive,
    string? Phone = null,
    string Timezone = "Asia/Ho_Chi_Minh",
    string CurrencyCode = "VND",
    string? TaxCode = null,
    string? ReceiptHeader = null,
    string? ReceiptFooter = null,
    DateTimeOffset? CreatedAt = null,
    DateTimeOffset? UpdatedAt = null
);
