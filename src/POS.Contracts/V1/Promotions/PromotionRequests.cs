using POS.Contracts.V1.Common;

namespace POS.Contracts.V1.Promotions;

public record PromotionFilterRequest(
    Guid? StoreId = null,
    string? Status = null,
    string? Type = null,
    DateTimeOffset? ActiveAt = null,
    int PageNumber = 1,
    int PageSize = 20
) : PagedRequest(PageNumber, PageSize);

public record CreatePromotionRequest(
    string Name,
    string Type,
    decimal Value,
    decimal MinOrderAmount = 0,
    decimal? MaxDiscountAmount = null,
    string? ConditionsJson = null,
    int Priority = 0,
    bool IsStackable = false,
    bool IsExclusive = false,
    string AppliesTo = "All",
    DateTimeOffset? ValidFrom = null,
    DateTimeOffset? ValidTo = null,
    IReadOnlyList<Guid>? TargetCategoryIds = null,
    IReadOnlyList<Guid>? TargetSkuIds = null
);

public record UpdatePromotionRequest(
    string Name,
    string Type,
    decimal Value,
    decimal MinOrderAmount = 0,
    decimal? MaxDiscountAmount = null,
    string? ConditionsJson = null,
    int Priority = 0,
    bool IsStackable = false,
    bool IsExclusive = false,
    string AppliesTo = "All",
    DateTimeOffset? ValidFrom = null,
    DateTimeOffset? ValidTo = null,
    string Status = "Active",
    IReadOnlyList<Guid>? TargetCategoryIds = null,
    IReadOnlyList<Guid>? TargetSkuIds = null
);

public record VoucherFilterRequest(
    string? Code = null,
    bool? IsActive = null,
    int PageNumber = 1,
    int PageSize = 20
) : PagedRequest(PageNumber, PageSize);

public record CreateVoucherRequest(
    string Code,
    int MaxUses,
    int PerCustomerLimit = 1,
    DateTimeOffset? ExpiresAt = null
);

public record UpdateVoucherRequest(
    int MaxUses,
    int PerCustomerLimit,
    DateTimeOffset? ExpiresAt,
    bool IsActive
);

public record ValidateVoucherRequest(
    decimal OrderSubtotal,
    Guid? CustomerId = null
);
