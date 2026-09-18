using FluentValidation;

namespace POS.Application.UseCases.Employees.Queries.GetEmployeeLoginHistory;

public sealed class GetEmployeeLoginHistoryQueryValidator : AbstractValidator<GetEmployeeLoginHistoryQuery>
{
    public GetEmployeeLoginHistoryQueryValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.PageNumber).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}
