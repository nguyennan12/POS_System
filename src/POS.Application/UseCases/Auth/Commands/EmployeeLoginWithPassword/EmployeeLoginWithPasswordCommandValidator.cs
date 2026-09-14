using FluentValidation;

namespace POS.Application.UseCases.Auth.Commands.EmployeeLoginWithPassword;

public class EmployeeLoginWithPasswordCommandValidator : AbstractValidator<EmployeeLoginWithPasswordCommand>
{
  public EmployeeLoginWithPasswordCommandValidator()
  {
    RuleFor(x => x.Username)
      .NotEmpty().WithMessage("Tên đăng nhập không được để trống.")
      .MaximumLength(50).WithMessage("Tên đăng nhập tối đa 50 ký tự.");

    RuleFor(x => x.Password)
      .NotEmpty().WithMessage("Mật khẩu không được để trống.");
  }
}
