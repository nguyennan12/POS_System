using FluentValidation;

namespace POS.Application.UseCases.Stores.Commands.AssignAdminToStore;

public class AssignAdminToStoreCommandValidator : AbstractValidator<AssignAdminToStoreCommand>
{
    public AssignAdminToStoreCommandValidator()
    {
        RuleFor(x => x.StoreId)
            .NotEmpty().WithMessage("Id cửa hàng không hợp lệ.");
        RuleFor(x => x.EmployeeId)
            .NotEmpty().WithMessage("Id nhân viên không hợp lệ.");
    }
}
