using FluentValidation;

namespace POS.Application.UseCases.Stores.Commands.GrantOwnerAccess;

public class GrantOwnerAccessCommandValidator : AbstractValidator<GrantOwnerAccessCommand>
{
    public GrantOwnerAccessCommandValidator()
    {
        RuleFor(x => x.StoreId).NotEmpty();
        RuleFor(x => x.EmployeeId).NotEmpty();
    }
}
