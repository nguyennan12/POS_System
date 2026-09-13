using FluentValidation;

namespace POS.Application.UseCases.Suppliers.Commands.CreateSupplierPayment;

public class CreateSupplierPaymentCommandValidator : AbstractValidator<CreateSupplierPaymentCommand>
{
    private static readonly string[] ValidMethods = ["Cash", "BankTransfer", "Other"];

    public CreateSupplierPaymentCommandValidator()
    {
        RuleFor(x => x.SupplierId)
            .NotEmpty().WithMessage("Id nhà cung cấp không hợp lệ.");

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("Số tiền phải lớn hơn 0.");

        RuleFor(x => x.Method)
            .NotEmpty().WithMessage("Phương thức thanh toán không được để trống.")
            .Must(m => ValidMethods.Contains(m, StringComparer.OrdinalIgnoreCase))
            .WithMessage($"Phương thức thanh toán phải là một trong: {string.Join(", ", ValidMethods)}.");

        RuleFor(x => x.Note)
            .MaximumLength(500).WithMessage("Ghi chú tối đa 500 ký tự.")
            .When(x => x.Note is not null);
    }
}
