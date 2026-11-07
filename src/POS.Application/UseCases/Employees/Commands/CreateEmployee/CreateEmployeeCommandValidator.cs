using FluentValidation;

namespace POS.Application.UseCases.Employees.Commands.CreateEmployee;

public sealed class CreateEmployeeCommandValidator : AbstractValidator<CreateEmployeeCommand>
{
    public CreateEmployeeCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Username).NotEmpty().Must(x => x != null && x.Trim().Length <= 50);
        RuleFor(x => x.Password).NotEmpty();
        RuleFor(x => x.Pin).NotEmpty().Length(6).Matches("^[0-9]{6}$");
        RuleFor(x => x.Phone).MaximumLength(20).Matches(@"^\+?[0-9]{9,15}$").When(x => !string.IsNullOrEmpty(x.Phone));
        RuleFor(x => x.Email).MaximumLength(254).EmailAddress().When(x => !string.IsNullOrEmpty(x.Email));
        RuleFor(x => x.CitizenId).Matches("^[0-9]{12}$").When(x => !string.IsNullOrEmpty(x.CitizenId));
        RuleFor(x => x.HireDate).LessThanOrEqualTo(DateOnly.FromDateTime(DateTime.UtcNow)).When(x => x.HireDate.HasValue);
        RuleFor(x => x.HourlyWage).InclusiveBetween(0m, 9999999999999999m).When(x => x.HourlyWage.HasValue);
        RuleFor(x => x.EmploymentType).Must(x => x is null or "" or "Full-time" or "Part-time" or "Contract");
        RuleFor(x => x.RoleId).NotEmpty();
        RuleFor(x => x.StoreId).NotEqual(Guid.Empty);
    }
}
