using FluentValidation;

namespace POS.Application.UseCases.Stores.Commands.UpdateStore;

public class UpdateStoreCommandValidator : AbstractValidator<UpdateStoreCommand>
{
    public UpdateStoreCommandValidator()
    {
        RuleFor(x => x.StoreId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Address).MaximumLength(500);
        RuleFor(x => x.Phone).MaximumLength(20);
        RuleFor(x => x.Timezone).NotEmpty().MaximumLength(50)
            .Must(StoreValidation.IsValidTimezone).WithMessage("Múi giờ không hợp lệ.");
        RuleFor(x => x.CurrencyCode).NotEmpty().Length(3).Matches("^[A-Z]{3}$");
        RuleFor(x => x.TaxCode).MaximumLength(20);
    }
}
