using FluentValidation;

namespace POS.Application.UseCases.Employees.Commands.UpdateEmployee;

public sealed class UpdateEmployeeCommandValidator : AbstractValidator<UpdateEmployeeCommand>
{
    public UpdateEmployeeCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.RoleId).NotEmpty();
        RuleFor(x => x.StoreId).NotEqual(Guid.Empty);
    }
}
