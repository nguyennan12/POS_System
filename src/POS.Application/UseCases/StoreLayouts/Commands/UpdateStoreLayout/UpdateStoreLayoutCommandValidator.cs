using FluentValidation;

namespace POS.Application.UseCases.StoreLayouts.Commands.UpdateStoreLayout;

public class UpdateStoreLayoutCommandValidator : AbstractValidator<UpdateStoreLayoutCommand>
{
    public UpdateStoreLayoutCommandValidator()
    {
        RuleFor(x => x.LayoutId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.ExpectedRevision).GreaterThan(0);
    }
}
