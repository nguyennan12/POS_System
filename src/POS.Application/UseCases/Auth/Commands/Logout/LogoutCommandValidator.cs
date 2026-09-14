using FluentValidation;

namespace POS.Application.UseCases.Auth.Commands.Logout;

public class LogoutCommandValidator : AbstractValidator<LogoutCommand>
{
  public LogoutCommandValidator()
  {
    RuleFor(x => x.RefreshToken)
      .NotEmpty().WithMessage("Refresh token is required."); ;
  }
}