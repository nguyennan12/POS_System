using FluentValidation;

namespace POS.Application.UseCases.Employees.Commands.ResetPin;

public sealed class ResetPinCommandValidator : AbstractValidator<ResetPinCommand>
{
    public ResetPinCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.NewPin).NotEmpty().Length(6).Matches("^[0-9]{6}$");
    }
}
