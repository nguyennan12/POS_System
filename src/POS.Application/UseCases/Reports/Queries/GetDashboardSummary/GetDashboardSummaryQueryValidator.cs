using FluentValidation;

namespace POS.Application.UseCases.Reports.Queries.GetDashboardSummary;

public class GetDashboardSummaryQueryValidator : AbstractValidator<GetDashboardSummaryQuery>
{
    public GetDashboardSummaryQueryValidator()
    {
        RuleFor(x => x.Days)
            .InclusiveBetween(1, 90)
            .WithMessage("Khoảng thời gian biểu đồ phải từ 1 đến 90 ngày.");
    }
}
