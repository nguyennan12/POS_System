using FluentValidation;

namespace POS.Application.UseCases.Customers.Commands.RedeemPoints;

public class RedeemPointsCommandValidator : AbstractValidator<RedeemPointsCommand>
{
    /// <summary>
    /// Configures a required customer ID, positive points, and a maximum note length of 500 characters.
    /// </summary>
    public RedeemPointsCommandValidator()
    {
        RuleFor(x => x.CustomerId)
            .NotEmpty().WithMessage("Mã khách hàng không được để trống.");

        RuleFor(x => x.Points)
            .GreaterThan(0).WithMessage("Số điểm tiêu dùng phải lớn hơn 0.");

        RuleFor(x => x.Note)
            .MaximumLength(500).WithMessage("Ghi chú không được vượt quá 500 ký tự.");
    }
}
