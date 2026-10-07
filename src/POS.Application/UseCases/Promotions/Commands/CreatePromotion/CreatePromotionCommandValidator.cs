using FluentValidation;
using POS.Domain.Promotions.Enums;

namespace POS.Application.UseCases.Promotions.Commands.CreatePromotion;

public class CreatePromotionCommandValidator : AbstractValidator<CreatePromotionCommand>
{
    public CreatePromotionCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Tên chương trình khuyến mãi không được để trống.")
            .MaximumLength(200).WithMessage("Tên chương trình không được vượt quá 200 ký tự.");

        RuleFor(x => x.Type)
            .NotEmpty().WithMessage("Loại khuyến mãi không được để trống.")
            .Must(t => Enum.TryParse<PromotionType>(t, true, out _))
            .WithMessage("Loại khuyến mãi không hợp lệ (hỗ trợ: PercentSku, FixedSku, BuyXGetY, CartPercent, CartFixed, HappyHour).");

        RuleFor(x => x.Value)
            .GreaterThan(0).WithMessage("Giá trị khuyến mãi phải lớn hơn 0.");

        RuleFor(x => x)
            .Must(x => !string.Equals(x.Type, nameof(PromotionType.PercentSku), StringComparison.OrdinalIgnoreCase) &&
                       !string.Equals(x.Type, nameof(PromotionType.CartPercent), StringComparison.OrdinalIgnoreCase) ||
                       x.Value <= 100)
            .WithMessage("Giá trị giảm theo phần trăm không được vượt quá 100%.");

        RuleFor(x => x.MinOrderAmount)
            .GreaterThanOrEqualTo(0).WithMessage("Giá trị đơn hàng tối thiểu không được âm.");

        RuleFor(x => x.AppliesTo)
            .Must(a => Enum.TryParse<PromotionAppliesTo>(a, true, out _))
            .WithMessage("Phạm vi áp dụng không hợp lệ (hỗ trợ: All, Category, SKU).");

        RuleFor(x => x)
            .Must(x => !x.ValidFrom.HasValue || !x.ValidTo.HasValue || x.ValidFrom <= x.ValidTo)
            .WithMessage("Thời gian bắt đầu không được sau thời gian kết thúc.");

        RuleFor(x => x)
            .Must(x =>
            {
                if (string.Equals(x.AppliesTo, "Category", StringComparison.OrdinalIgnoreCase))
                {
                    return x.TargetCategoryIds != null && x.TargetCategoryIds.Count > 0;
                }
                if (string.Equals(x.AppliesTo, "SKU", StringComparison.OrdinalIgnoreCase))
                {
                    return x.TargetSkuIds != null && x.TargetSkuIds.Count > 0;
                }
                return true;
            })
            .WithMessage("Khuyến mãi theo Category hoặc SKU yêu cầu cung cấp danh sách mục tiêu áp dụng.");
    }
}
