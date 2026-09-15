

using FluentValidation;

namespace POS.Application.UseCases.Auth.Commands.Refresh;

public class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
  public RefreshTokenCommandValidator()
  {
    RuleFor(x => x.RefreshToken)
      .NotEmpty().WithMessage("Refresh token is required."); ;
  }
}