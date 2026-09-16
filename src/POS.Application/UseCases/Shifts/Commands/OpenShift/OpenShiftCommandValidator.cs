using FluentValidation;

namespace POS.Application.UseCases.Shifts.Commands.OpenShift;

public class OpenShiftCommandValidator : AbstractValidator<OpenShiftCommand>
{
    public OpenShiftCommandValidator()
    {
        RuleFor(x => x.StoreId)
            .NotEmpty().WithMessage("Mã cửa hàng không được để trống.");

        RuleFor(x => x.OpeningCash)
            .GreaterThanOrEqualTo(0).WithMessage("Số tiền đầu ca phải >= 0.");
    }
}
