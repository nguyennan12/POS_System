using FluentValidation;

namespace POS.Application.UseCases.Invoices.Queries.GetInvoices;

public class GetInvoicesQueryValidator : AbstractValidator<GetInvoicesQuery>
{
    public GetInvoicesQueryValidator()
    {
        RuleFor(x => x.Filter).NotNull();
        When(x => x.Filter is not null, () =>
        {
            RuleFor(x => x.Filter.PageNumber).GreaterThanOrEqualTo(1)
                .WithMessage("Số trang phải lớn hơn hoặc bằng 1.");
            RuleFor(x => x.Filter.PageSize).InclusiveBetween(1, 100)
                .WithMessage("Kích thước trang phải từ 1 đến 100.");
            RuleFor(x => x.Filter.OrderId).NotEqual(Guid.Empty).When(x => x.Filter.OrderId.HasValue)
                .WithMessage("Id đơn hàng không hợp lệ.");
            RuleFor(x => x.Filter.From).LessThanOrEqualTo(x => x.Filter.To)
                .When(x => x.Filter.From.HasValue && x.Filter.To.HasValue)
                .WithMessage("Thời điểm bắt đầu phải nhỏ hơn hoặc bằng thời điểm kết thúc.");
        });
    }
}
