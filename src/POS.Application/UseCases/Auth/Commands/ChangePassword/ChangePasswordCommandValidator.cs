using FluentValidation;

namespace POS.Application.UseCases.Auth.Commands.ChangePassword;

public sealed class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordCommandValidator()
    {
        RuleFor(x => x.OldPassword)
            .NotEmpty().WithMessage("Mật khẩu hiện tại không được để trống.");

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("Mật khẩu mới không được để trống.")
            .NotEqual(x => x.OldPassword).WithMessage("Mật khẩu mới không được trùng với mật khẩu hiện tại.");
    }
}
