using FluentValidation;

namespace POS.Application.UseCases.Inventory.Stock.Commands.DisposeStock;

public class DisposeStockCommandValidator : AbstractValidator<DisposeStockCommand>
{
    public DisposeStockCommandValidator()
    {
        RuleFor(x => x.SkuId)
            .NotEmpty().WithMessage("SKU không được để trống.");

        RuleFor(x => x.Qty)
            .GreaterThan(0).WithMessage("Số lượng xuất hủy phải lớn hơn 0.");

        RuleFor(x => x.Note)
            .NotEmpty().WithMessage("Ghi chú bắt buộc khi xuất hủy.")
            .MaximumLength(500).WithMessage("Ghi chú tối đa 500 ký tự.");
    }
}
