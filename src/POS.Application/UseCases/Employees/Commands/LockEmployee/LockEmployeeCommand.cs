using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;

namespace POS.Application.UseCases.Employees.Commands.LockEmployee;

public record LockEmployeeCommand(Guid Id, bool IsActive) : ICommand<EmployeeDto>, IRequirePermission
{
    public string RequiredPermission => "employees:update";
}
