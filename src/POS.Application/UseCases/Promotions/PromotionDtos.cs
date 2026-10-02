namespace POS.Application.UseCases.Promotions;

public record PromotionSummaryDto(
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

public record PromotionDetailDto(
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

public record VoucherDto(
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

public record ValidateVoucherResultDto(
    bool IsValid,
    string? ErrorMessage,
    decimal DiscountAmount,
    string? VoucherCode,
    Guid? PromotionId,
    string? PromotionName
);

public record PagedPromotionList(
    List<PromotionSummaryDto> Items,
    int TotalCount,
    int PageNumber,
    int PageSize
);

public record PagedVoucherList(
    List<VoucherDto> Items,
    int TotalCount,
    int PageNumber,
    int PageSize
);
