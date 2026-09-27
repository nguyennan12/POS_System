using FluentValidation;

namespace POS.Application.UseCases.Orders.Commands.AddOrderItem;

public class AddOrderItemCommandValidator : AbstractValidator<AddOrderItemCommand>
{
    public AddOrderItemCommandValidator()
    {
        RuleFor(x => x.OrderId)
            .NotEmpty().WithMessage("Mã đơn hàng không được để trống.");

        RuleFor(x => x.SkuId)
            .NotEmpty().WithMessage("Mã sản phẩm (SKU) không được để trống.");

        RuleFor(x => x.Qty)
            .GreaterThan(0).WithMessage("Số lượng sản phẩm phải lớn hơn 0.");
    }
}
