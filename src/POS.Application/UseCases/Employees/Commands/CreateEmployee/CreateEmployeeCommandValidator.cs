using FluentValidation;

namespace POS.Application.UseCases.Employees.Commands.CreateEmployee;

public sealed class CreateEmployeeCommandValidator : AbstractValidator<CreateEmployeeCommand>
{
    public CreateEmployeeCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Username).NotEmpty().Must(x => x != null && x.Trim().Length <= 50);
        RuleFor(x => x.Password).NotEmpty();
        RuleFor(x => x.Pin).NotEmpty().Length(6).Matches("^[0-9]{6}$");
        RuleFor(x => x.RoleId).NotEmpty();
        RuleFor(x => x.StoreId).NotEqual(Guid.Empty);
    }
}
