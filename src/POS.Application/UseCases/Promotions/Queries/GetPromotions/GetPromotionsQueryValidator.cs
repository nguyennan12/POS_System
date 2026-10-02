using FluentValidation;
using POS.Domain.Promotions.Enums;

namespace POS.Application.UseCases.Promotions.Queries.GetPromotions;

public class GetPromotionsQueryValidator : AbstractValidator<GetPromotionsQuery>
{
    public GetPromotionsQueryValidator()
    {
        RuleFor(x => x.PageNumber)
            .GreaterThanOrEqualTo(1).WithMessage("Số trang phải lớn hơn hoặc bằng 1.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100).WithMessage("Kích thước trang phải từ 1 đến 100.");

        RuleFor(x => x.Status)
            .Must(s => string.IsNullOrWhiteSpace(s) || Enum.TryParse<PromotionStatus>(s, true, out _))
            .WithMessage("Trạng thái khuyến mãi không hợp lệ (hỗ trợ: Active, Inactive).");

        RuleFor(x => x.Type)
            .Must(t => string.IsNullOrWhiteSpace(t) || Enum.TryParse<PromotionType>(t, true, out _))
            .WithMessage("Loại khuyến mãi không hợp lệ.");
    }
}
