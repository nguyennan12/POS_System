using FluentValidation;

namespace POS.Application.UseCases.Rbac.Commands.CreateRole;

public class CreateRoleCommandValidator : AbstractValidator<CreateRoleCommand>
{
  public CreateRoleCommandValidator()
  {
    RuleFor(x => x.Name)
        .NotEmpty().WithMessage("Tên vai trò không được để trống.")
        .MaximumLength(100).WithMessage("Tên vai trò không được vượt quá 100 ký tự.");

    RuleFor(x => x.Description)
        .MaximumLength(500).WithMessage("Mô tả không được vượt quá 500 ký tự.")
        .When(x => !string.IsNullOrEmpty(x.Description));
  }
}
