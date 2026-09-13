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
    RuleFor(x => x.DeviceId)
        .NotEmpty()
        .WithMessage("DeviceId không được để trống.")
        .MaximumLength(100)
        .WithMessage("DeviceId tối đa 100 ký tự.");
  }
}
