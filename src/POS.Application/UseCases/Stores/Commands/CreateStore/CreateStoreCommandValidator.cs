using FluentValidation;

namespace POS.Application.UseCases.Stores.Commands.CreateStore;

public class CreateStoreCommandValidator : AbstractValidator<CreateStoreCommand>
{
    public CreateStoreCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Tên cửa hàng không được để trống.")
            .MaximumLength(200).WithMessage("Tên cửa hàng tối đa 200 ký tự.");

        RuleFor(x => x.CurrencyCode)
            .NotEmpty().WithMessage("Đơn vị tiền tệ không được để trống.")
            .Length(3).Matches("^[A-Z]{3}$").WithMessage("Mã tiền tệ gồm 3 chữ cái in hoa.");
        RuleFor(x => x.Address).MaximumLength(500);
        RuleFor(x => x.Phone).MaximumLength(20);
        RuleFor(x => x.Timezone).NotEmpty().MaximumLength(50)
            .Must(StoreValidation.IsValidTimezone).WithMessage("Múi giờ không hợp lệ.");
    }
}
