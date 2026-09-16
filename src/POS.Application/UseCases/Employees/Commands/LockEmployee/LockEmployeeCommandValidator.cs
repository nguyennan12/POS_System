using FluentValidation;

namespace POS.Application.UseCases.Employees.Commands.LockEmployee;

public sealed class LockEmployeeCommandValidator : AbstractValidator<LockEmployeeCommand>
{
    public LockEmployeeCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
