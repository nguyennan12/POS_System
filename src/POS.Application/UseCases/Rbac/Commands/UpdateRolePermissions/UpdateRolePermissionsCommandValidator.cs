using FluentValidation;

namespace POS.Application.UseCases.Rbac.Commands.UpdateRolePermissions;

public class UpdateRolePermissionsCommandValidator : AbstractValidator<UpdateRolePermissionsCommand>
{
    public UpdateRolePermissionsCommandValidator()
    {
        RuleFor(x => x.RoleId)
            .NotEmpty().WithMessage("Mã vai trò không được để trống.");

        RuleFor(x => x.PermissionIds)
            .NotNull().WithMessage("Danh sách mã quyền không được để null.");
    }
}
