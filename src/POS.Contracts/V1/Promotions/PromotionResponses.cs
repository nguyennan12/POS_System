namespace POS.Contracts.V1.Promotions;

public record PromotionSummaryResponse(
    Guid Id,
    Guid StoreId,
    string Name,
    string Type,
    decimal Value,
    string AppliesTo,
    DateTimeOffset ValidFrom,
    DateTimeOffset? ValidTo,
    string Status,
    DateTimeOffset CreatedAt
);

public record PromotionDetailResponse(
    Guid Id,
    Guid StoreId,
    string Name,
    string Type,
    decimal Value,
    decimal MinOrderAmount,
    decimal? MaxDiscountAmount,
    string? ConditionsJson,
    int Priority,
    bool IsStackable,
    bool IsExclusive,
    string AppliesTo,
    DateTimeOffset ValidFrom,
    DateTimeOffset? ValidTo,
    string Status,
    IReadOnlyList<Guid> TargetCategoryIds,
    IReadOnlyList<Guid> TargetSkuIds,
    DateTimeOffset CreatedAt
);

public record VoucherResponse(
    Guid Id,
    Guid PromotionId,
    string? PromotionName,
    string Code,
    int MaxUses,
    int UsedCount,
    int PerCustomerLimit,
    DateTimeOffset? ExpiresAt,
    bool IsActive
);

public record ValidateVoucherResponse(
    bool IsValid,
    string? ErrorMessage,
    decimal DiscountAmount,
    string? VoucherCode,
    Guid? PromotionId,
    string? PromotionName
);
