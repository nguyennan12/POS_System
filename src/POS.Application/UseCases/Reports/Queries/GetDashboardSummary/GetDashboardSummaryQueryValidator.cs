using FluentValidation;

namespace POS.Application.UseCases.Reports.Queries.GetDashboardSummary;

public class GetDashboardSummaryQueryValidator : AbstractValidator<GetDashboardSummaryQuery>
{
    public GetDashboardSummaryQueryValidator()
    {
        RuleFor(x => x.Days)
            .InclusiveBetween(7, 30)
            .WithMessage("Khoảng thời gian biểu đồ phải từ 7 đến 30 ngày.")
            .When(x => !x.CurrentStoreOnly);

        RuleFor(x => x.Days)
            .InclusiveBetween(1, 90)
            .When(x => x.CurrentStoreOnly);
    }
}
