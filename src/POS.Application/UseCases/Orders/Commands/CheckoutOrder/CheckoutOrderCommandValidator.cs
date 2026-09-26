using FluentValidation;
using POS.Domain.Orders.Enums;

namespace POS.Application.UseCases.Orders.Commands.CheckoutOrder;

public class CheckoutOrderCommandValidator : AbstractValidator<CheckoutOrderCommand>
{
    public CheckoutOrderCommandValidator()
    {
        RuleFor(x => x.OrderId)
            .NotEmpty().WithMessage("Mã đơn hàng không được để trống.");

        RuleFor(x => x.Payments)
            .NotNull().WithMessage("Danh sách phương thức thanh toán không được để trống.")
            .Must(p => p != null && p.Count > 0).WithMessage("Vui lòng cung cấp ít nhất một phương thức thanh toán.");

        RuleForEach(x => x.Payments).ChildRules(p =>
        {
            p.RuleFor(x => x.Amount)
                .GreaterThan(0).WithMessage("Số tiền thanh toán phải lớn hơn 0.");

            p.RuleFor(x => x.Method)
                .NotEmpty().WithMessage("Phương thức thanh toán không được để trống.")
                .Must(BeAValidPaymentMethod).WithMessage("Phương thức thanh toán không hợp lệ. Hỗ trợ: Cash, MoMo, VietQR, Card, Points.");
        });
    }

    private static bool BeAValidPaymentMethod(string method)
    {
        return Enum.TryParse<PaymentMethod>(method, ignoreCase: true, out _);
    }
}
