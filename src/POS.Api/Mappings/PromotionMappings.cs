using POS.Application.UseCases.Promotions;
using POS.Contracts.V1.Promotions;

namespace POS.Api.Mappings;

public static class PromotionMappings
{
    public static PromotionSummaryResponse ToSummaryResponse(this PromotionSummaryDto dto) =>
        new(
            dto.Id,
            dto.StoreId,
            dto.Name,
            dto.Type,
            dto.Value,
            dto.AppliesTo,
            dto.ValidFrom,
            dto.ValidTo,
            dto.Status,
            dto.CreatedAt
        );

    public static PromotionDetailResponse ToDetailResponse(this PromotionDetailDto dto) =>
        new(
            dto.Id,
            dto.StoreId,
            dto.Name,
            dto.Type,
            dto.Value,
            dto.MinOrderAmount,
            dto.MaxDiscountAmount,
            dto.ConditionsJson,
            dto.Priority,
            dto.IsStackable,
            dto.IsExclusive,
            dto.AppliesTo,
            dto.ValidFrom,
            dto.ValidTo,
            dto.Status,
            dto.TargetCategoryIds,
            dto.TargetSkuIds,
            dto.CreatedAt
        );

    public static VoucherResponse ToResponse(this VoucherDto dto) =>
        new(
            dto.Id,
            dto.PromotionId,
            dto.PromotionName,
            dto.Code,
            dto.MaxUses,
            dto.UsedCount,
            dto.PerCustomerLimit,
            dto.ExpiresAt,
            dto.IsActive
        );

    public static ValidateVoucherResponse ToResponse(this ValidateVoucherResultDto dto) =>
        new(
            dto.IsValid,
            dto.ErrorMessage,
            dto.DiscountAmount,
            dto.VoucherCode,
            dto.PromotionId,
            dto.PromotionName
        );
}
