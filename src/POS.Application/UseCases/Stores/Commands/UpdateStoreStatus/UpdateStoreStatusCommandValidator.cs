using FluentValidation;

namespace POS.Application.UseCases.Stores.Commands.UpdateStoreStatus;

public class UpdateStoreStatusCommandValidator : AbstractValidator<UpdateStoreStatusCommand>
{
    public UpdateStoreStatusCommandValidator()
    {
        RuleFor(x => x.StoreId)
            .NotEmpty().WithMessage("Id cửa hàng không hợp lệ.");
        RuleFor(x => x.IsActive)
            .NotNull().WithMessage("Trạng thái hoạt động của cửa hàng không được để trống.");
    }
}
