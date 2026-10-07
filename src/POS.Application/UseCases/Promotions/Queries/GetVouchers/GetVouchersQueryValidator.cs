using FluentValidation;

namespace POS.Application.UseCases.Promotions.Queries.GetVouchers;

public class GetVouchersQueryValidator : AbstractValidator<GetVouchersQuery>
{
    public GetVouchersQueryValidator()
    {
        RuleFor(x => x.PageNumber)
            .GreaterThanOrEqualTo(1).WithMessage("Số trang phải lớn hơn hoặc bằng 1.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100).WithMessage("Kích thước trang phải từ 1 đến 100.");
    }
}
