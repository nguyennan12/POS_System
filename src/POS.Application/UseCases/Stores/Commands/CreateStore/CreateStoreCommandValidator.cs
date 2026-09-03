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
            .MaximumLength(10).WithMessage("Mã tiền tệ tối đa 10 ký tự.");
    }
}
