using FluentValidation;

namespace POS.Application.UseCases.Suppliers.Queries.GetSuppliers;

public class GetSuppliersQueryValidator : AbstractValidator<GetSuppliersQuery>
{
    public GetSuppliersQueryValidator()
    {
        RuleFor(x => x.PageNumber)
            .GreaterThanOrEqualTo(1).WithMessage("Số trang phải lớn hơn hoặc bằng 1.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100).WithMessage("Kích thước trang phải từ 1 đến 100.");

        RuleFor(x => x.Search)
            .MaximumLength(200).WithMessage("Từ khóa tìm kiếm tối đa 200 ký tự.")
            .When(x => x.Search is not null);
    }
}
