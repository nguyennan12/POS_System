using FluentValidation;

namespace POS.Application.UseCases.Orders.Commands.CancelOrder;

public class CancelOrderCommandValidator : AbstractValidator<CancelOrderCommand>
{
    public CancelOrderCommandValidator()
    {
        RuleFor(x => x.OrderId)
            .NotEmpty().WithMessage("Mã đơn hàng không được để trống.");

        RuleFor(x => x.Reason)
            .MaximumLength(500).WithMessage("Lý do hủy đơn không được vượt quá 500 ký tự.");
    }
}
