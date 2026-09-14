using FluentValidation;

namespace POS.Application.UseCases.Stores.Commands.AssignAdminToStore;

public class AssignAdminToStoreCommandValidator : AbstractValidator<AssignAdminToStoreCommand>
{
    public AssignAdminToStoreCommandValidator()
    {
        RuleFor(x => x.StoreId).NotEmpty();
        RuleFor(x => x.EmployeeId).NotEmpty();
    }
}
