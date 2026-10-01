using FluentValidation;

namespace POS.Application.UseCases.Auth.Commands.ChangePin;

public sealed class ChangePinCommandValidator : AbstractValidator<ChangePinCommand>
{
    public ChangePinCommandValidator()
    {
        RuleFor(x => x.OldPin)
            .NotEmpty().WithMessage("Mã PIN hiện tại không được để trống.")
            .Length(6).WithMessage("Mã PIN phải có đúng 6 chữ số.")
            .Matches("^[0-9]{6}$").WithMessage("Mã PIN chỉ được chứa ký tự số.");

        RuleFor(x => x.NewPin)
            .NotEmpty().WithMessage("Mã PIN mới không được để trống.")
            .Length(6).WithMessage("Mã PIN phải có đúng 6 chữ số.")
            .Matches("^[0-9]{6}$").WithMessage("Mã PIN chỉ được chứa ký tự số.")
            .NotEqual(x => x.OldPin).WithMessage("Mã PIN mới không được trùng với mã PIN hiện tại.");
    }
}
