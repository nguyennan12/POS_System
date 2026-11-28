using FluentValidation;

namespace POS.Application.UseCases.StoreLayouts.Commands.CreateStoreLayout;

public class CreateStoreLayoutCommandValidator : AbstractValidator<CreateStoreLayoutCommand>
{
    public CreateStoreLayoutCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.GridWidth).InclusiveBetween(4, 100);
        RuleFor(x => x.GridHeight).InclusiveBetween(4, 100);
    }
}
