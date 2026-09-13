using FluentValidation;

namespace POS.Application.UseCases.Auth.Commands.EmployeeLoginWithPin;

public class EmployeeLoginWithPinCommandValidator : AbstractValidator<EmployeeLoginWithPinCommand>
{
  public EmployeeLoginWithPinCommandValidator()
  {
    RuleFor(x => x.StoreId)
          .NotEmpty()
          .WithMessage("StoreId không được để trống.");

    RuleFor(x => x.Pin)
        .NotEmpty()
        .WithMessage("PIN không được để trống.")
        .Matches(@"^\d{6}$")
        .WithMessage("PIN phải gồm đúng 6 chữ số.");
  }
}
