using FluentValidation;

namespace POS.Application.UseCases.Shifts.Commands.CloseShift;

public class CloseShiftCommandValidator : AbstractValidator<CloseShiftCommand>
{
    public CloseShiftCommandValidator()
    {
        RuleFor(x => x.ShiftId)
            .NotEmpty().WithMessage("Mã ca làm việc không được để trống.");

        RuleFor(x => x.ActualCash)
            .GreaterThanOrEqualTo(0).WithMessage("Số tiền thực tế phải >= 0.");
    }
}
