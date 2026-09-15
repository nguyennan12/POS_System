using FluentValidation;

namespace POS.Application.UseCases.Stores.Commands.GrantOwnerAccess;

public class GrantOwnerAccessCommandValidator : AbstractValidator<GrantOwnerAccessCommand>
{
    public GrantOwnerAccessCommandValidator()
    {
        RuleFor(x => x.StoreId)
            .NotEmpty().WithMessage("Id cửa hàng không hợp lệ.");
        RuleFor(x => x.EmployeeId)
            .NotEmpty().WithMessage("Id nhân viên không hợp lệ.");
    }
}
