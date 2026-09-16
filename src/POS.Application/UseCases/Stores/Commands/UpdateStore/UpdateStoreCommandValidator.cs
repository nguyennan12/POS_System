using FluentValidation;

namespace POS.Application.UseCases.Stores.Commands.UpdateStore;

public class UpdateStoreCommandValidator : AbstractValidator<UpdateStoreCommand>
{
    public UpdateStoreCommandValidator()
    {
        RuleFor(x => x.StoreId)
            .NotEmpty().WithMessage("Id cửa hàng không hợp lệ.");
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Tên cửa hàng không được để trống.")
            .MaximumLength(200).WithMessage("Tên cửa hàng tối đa 200 ký tự.");
        RuleFor(x => x.Address)
            .MaximumLength(500).WithMessage("Địa chỉ cửa hàng tối đa 500 ký tự.");
        RuleFor(x => x.Phone)
            .MaximumLength(20).WithMessage("Số điện thoại cửa hàng tối đa 20 ký tự.");
        RuleFor(x => x.Timezone)
            .NotEmpty().WithMessage("Múi giờ không được để trống.")
            .MaximumLength(50).WithMessage("Múi giờ tối đa 50 ký tự.")
            .Must(StoreValidation.IsValidTimezone).WithMessage("Múi giờ không hợp lệ.");
        RuleFor(x => x.CurrencyCode)
            .NotEmpty().WithMessage("Đơn vị tiền tệ không được để trống.")
            .Length(3).Matches("^[A-Z]{3}$").WithMessage("Mã tiền tệ gồm 3 chữ cái in hoa.");
        RuleFor(x => x.TaxCode)
            .MaximumLength(20).WithMessage("Mã số thuế tối đa 20 ký tự.");
    }
}
