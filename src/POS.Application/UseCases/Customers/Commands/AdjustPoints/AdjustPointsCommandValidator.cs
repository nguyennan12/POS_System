using FluentValidation;

namespace POS.Application.UseCases.Customers.Commands.AdjustPoints;

public class AdjustPointsCommandValidator : AbstractValidator<AdjustPointsCommand>
{

  /// Configures a required customer ID, nonzero points, and a required note of at most 500 characters.
  /// </summary>
  public AdjustPointsCommandValidator()
  {
    RuleFor(x => x.CustomerId)
        .NotEmpty().WithMessage("Mã khách hàng không được để trống.");

    RuleFor(x => x.Points)
        .NotEqual(0).WithMessage("Số điểm điều chỉnh phải khác 0.");

    RuleFor(x => x.Note)
        .NotEmpty().WithMessage("Lý do/ghi chú điều chỉnh điểm không được để trống.")
        .MaximumLength(500).WithMessage("Ghi chú không được vượt quá 500 ký tự.");
  }
}
