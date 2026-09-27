using FluentValidation;

namespace POS.Application.UseCases.Orders.Commands.CreateOrder;

public class CreateOrderCommandValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderCommandValidator()
    {
        RuleFor(x => x.ShiftId)
            .NotEmpty().WithMessage("Mã ca làm việc không được để trống.");
    }
}
