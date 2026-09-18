using FluentValidation;

namespace POS.Application.UseCases.Employees.Queries.GetEmployeeDetail;

public sealed class GetEmployeeDetailQueryValidator : AbstractValidator<GetEmployeeDetailQuery>
{
    public GetEmployeeDetailQueryValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
