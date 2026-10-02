using MediatR;
using POS.Application.Abstractions.Persistence;
using POS.Domain.Common;
using POS.Domain.Promotions.Enums;

namespace POS.Application.UseCases.Promotions.Queries.ValidateVoucher;

public class ValidateVoucherQueryHandler(IVoucherRepository voucherRepository)
    : IRequestHandler<ValidateVoucherQuery, Result<ValidateVoucherResultDto>>
{
    public async Task<Result<ValidateVoucherResultDto>> Handle(ValidateVoucherQuery request, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var normalizedCode = request.Code.Trim().ToUpper();

        var voucher = await voucherRepository.GetByCodeWithPromotionAsync(normalizedCode, cancellationToken);
        if (voucher is null)
        {
            return new ValidateVoucherResultDto(
                IsValid: false,
                ErrorMessage: "Mã giảm giá không tồn tại.",
                DiscountAmount: 0,
                VoucherCode: normalizedCode,
                PromotionId: null,
                PromotionName: null
            );
        }

        if (!voucher.IsActive)
        {
            return new ValidateVoucherResultDto(
                IsValid: false,
                ErrorMessage: "Mã giảm giá đang bị tạm khóa.",
                DiscountAmount: 0,
                VoucherCode: voucher.Code,
                PromotionId: voucher.PromotionId,
                PromotionName: voucher.Promotion?.Name
            );
        }

        if (voucher.ExpiresAt.HasValue && now > voucher.ExpiresAt.Value)
        {
            return new ValidateVoucherResultDto(
                IsValid: false,
                ErrorMessage: "Mã giảm giá đã hết hạn sử dụng.",
                DiscountAmount: 0,
                VoucherCode: voucher.Code,
                PromotionId: voucher.PromotionId,
                PromotionName: voucher.Promotion?.Name
            );
        }

        if (voucher.UsedCount >= voucher.MaxUses)
        {
            return new ValidateVoucherResultDto(
                IsValid: false,
                ErrorMessage: "Mã giảm giá đã hết lượt sử dụng.",
                DiscountAmount: 0,
                VoucherCode: voucher.Code,
                PromotionId: voucher.PromotionId,
                PromotionName: voucher.Promotion?.Name
            );
        }

        if (request.CustomerId.HasValue)
        {
            var customerUsageCount = await voucherRepository.GetCustomerUsageCountAsync(
                voucher.Id, request.CustomerId.Value, cancellationToken);

            if (customerUsageCount >= voucher.PerCustomerLimit)
            {
                return new ValidateVoucherResultDto(
                    IsValid: false,
                    ErrorMessage: "Khách hàng đã dùng hết số lượt cho phép với mã giảm giá này.",
                    DiscountAmount: 0,
                    VoucherCode: voucher.Code,
                    PromotionId: voucher.PromotionId,
                    PromotionName: voucher.Promotion?.Name
                );
            }
        }

        var promotion = voucher.Promotion;
        if (promotion is null || promotion.Status != PromotionStatus.Active || !promotion.IsActiveAt(now))
        {
            return new ValidateVoucherResultDto(
                IsValid: false,
                ErrorMessage: "Chương trình khuyến mãi không còn hiệu lực.",
                DiscountAmount: 0,
                VoucherCode: voucher.Code,
                PromotionId: voucher.PromotionId,
                PromotionName: voucher.Promotion?.Name
            );
        }

        if (request.StoreId.HasValue && promotion.StoreId != Guid.Empty && promotion.StoreId != request.StoreId.Value)
        {
            return new ValidateVoucherResultDto(
                IsValid: false,
                ErrorMessage: "Mã giảm giá không áp dụng cho cửa hàng này.",
                DiscountAmount: 0,
                VoucherCode: voucher.Code,
                PromotionId: voucher.PromotionId,
                PromotionName: voucher.Promotion?.Name
            );
        }

        if (request.OrderSubtotal < promotion.MinOrderAmount)
        {
            return new ValidateVoucherResultDto(
                IsValid: false,
                ErrorMessage: $"Đơn hàng chưa đạt giá trị tối thiểu {promotion.MinOrderAmount:N0} đ để áp dụng mã giảm giá.",
                DiscountAmount: 0,
                VoucherCode: voucher.Code,
                PromotionId: voucher.PromotionId,
                PromotionName: voucher.Promotion?.Name
            );
        }

        decimal discount = 0;
        if (promotion.Type == PromotionType.CartPercent)
        {
            discount = request.OrderSubtotal * (promotion.Value / 100m);
            if (promotion.MaxDiscountAmount.HasValue && discount > promotion.MaxDiscountAmount.Value)
            {
                discount = promotion.MaxDiscountAmount.Value;
            }
        }
        else if (promotion.Type == PromotionType.CartFixed)
        {
            discount = Math.Min(promotion.Value, request.OrderSubtotal);
        }
        else
        {
            discount = promotion.MaxDiscountAmount.HasValue
                ? Math.Min(promotion.Value, promotion.MaxDiscountAmount.Value)
                : promotion.Value;
        }

        discount = Math.Min(discount, request.OrderSubtotal);
        discount = Math.Max(0, discount);

        return new ValidateVoucherResultDto(
            IsValid: true,
            ErrorMessage: null,
            DiscountAmount: discount,
            VoucherCode: voucher.Code,
            PromotionId: promotion.Id,
            PromotionName: promotion.Name
        );
    }
}
