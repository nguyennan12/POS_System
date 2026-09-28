using FluentValidation;

namespace POS.Application.UseCases.Inventory.StockIn.Commands.CreateStockInVoucher;

public class CreateStockInVoucherCommandValidator : AbstractValidator<CreateStockInVoucherCommand>
{
    public CreateStockInVoucherCommandValidator()
    {
        RuleFor(x => x.SupplierId)
            .NotEmpty().WithMessage("Nhà cung cấp không được để trống.");

        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("Phiếu nhập phải có ít nhất một mặt hàng.");

        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.SkuId)
                .NotEmpty().WithMessage("SKU không được để trống.");
            item.RuleFor(i => i.Qty)
                .GreaterThan(0).WithMessage("Số lượng phải lớn hơn 0.");
            item.RuleFor(i => i.UnitPrice)
                .GreaterThanOrEqualTo(0).WithMessage("Đơn giá không được âm.");
        });

        RuleFor(x => x.Note)
            .MaximumLength(500).WithMessage("Ghi chú tối đa 500 ký tự.")
            .When(x => x.Note is not null);
    }
}
