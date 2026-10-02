using FluentValidation;
using POS.Domain.Promotions.Enums;

namespace POS.Application.UseCases.Promotions.Commands.UpdatePromotion;

public class UpdatePromotionCommandValidator : AbstractValidator<UpdatePromotionCommand>
{
    public UpdatePromotionCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Mã khuyến mãi không được để trống.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Tên chương trình khuyến mãi không được để trống.")
            .MaximumLength(200).WithMessage("Tên chương trình không được vượt quá 200 ký tự.");

        RuleFor(x => x.Type)
            .NotEmpty().WithMessage("Loại khuyến mãi không được để trống.")
            .Must(t => Enum.TryParse<PromotionType>(t, true, out _))
            .WithMessage("Loại khuyến mãi không hợp lệ.");

        RuleFor(x => x.Value)
            .GreaterThan(0).WithMessage("Giá trị khuyến mãi phải lớn hơn 0.");

        RuleFor(x => x)
            .Must(x => !x.Type.Equals(nameof(PromotionType.PercentSku), StringComparison.OrdinalIgnoreCase) &&
                       !x.Type.Equals(nameof(PromotionType.CartPercent), StringComparison.OrdinalIgnoreCase) ||
                       x.Value <= 100)
            .WithMessage("Giá trị giảm theo phần trăm không được vượt quá 100%.");

        RuleFor(x => x.MinOrderAmount)
            .GreaterThanOrEqualTo(0).WithMessage("Giá trị đơn hàng tối thiểu không được âm.");

        RuleFor(x => x.AppliesTo)
            .Must(a => Enum.TryParse<PromotionAppliesTo>(a, true, out _))
            .WithMessage("Phạm vi áp dụng không hợp lệ.");

        RuleFor(x => x.Status)
            .Must(s => Enum.TryParse<PromotionStatus>(s, true, out _))
            .WithMessage("Trạng thái không hợp lệ.");

        RuleFor(x => x)
            .Must(x => !x.ValidFrom.HasValue || !x.ValidTo.HasValue || x.ValidFrom <= x.ValidTo)
            .WithMessage("Thời gian bắt đầu không được sau thời gian kết thúc.");

        RuleFor(x => x)
            .Must(x =>
            {
                if (x.AppliesTo.Equals("Category", StringComparison.OrdinalIgnoreCase))
                {
                    return x.TargetCategoryIds != null && x.TargetCategoryIds.Count > 0;
                }
                if (x.AppliesTo.Equals("SKU", StringComparison.OrdinalIgnoreCase))
                {
                    return x.TargetSkuIds != null && x.TargetSkuIds.Count > 0;
                }
                return true;
            })
            .WithMessage("Khuyến mãi theo Category hoặc SKU yêu cầu cung cấp danh sách mục tiêu áp dụng.");
    }
}
