using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;

namespace POS.Application.UseCases.Employees.Commands.ResetPassword;

public record ResetPasswordCommand(Guid Id, string NewPassword) : ICommand<EmployeeDto>, IRequirePermission
{
    public string RequiredPermission => "employees:update";
}
