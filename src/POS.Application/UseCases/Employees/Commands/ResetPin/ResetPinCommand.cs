using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;

namespace POS.Application.UseCases.Employees.Commands.ResetPin;

public record ResetPinCommand(Guid Id, string NewPin) : ICommand<EmployeeDto>, IRequirePermission
{
    public string RequiredPermission => "employees:update";
}
