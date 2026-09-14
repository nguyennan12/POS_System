using FluentValidation;

namespace POS.Application.UseCases.Stores.Commands.UpdateStoreStatus;

public class UpdateStoreStatusCommandValidator : AbstractValidator<UpdateStoreStatusCommand>
{
    public UpdateStoreStatusCommandValidator()
    {
        RuleFor(x => x.StoreId).NotEmpty();
    }
}
