using FluentValidation;

namespace POS.Application.UseCases.Categories.Commands.CreateCategory;

public class CreateCategoryCommandValidator : AbstractValidator<CreateCategoryCommand>
{
    public CreateCategoryCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100);
            
        RuleFor(x => x.DisplayOrder)
            .GreaterThanOrEqualTo(0);
    }
}
