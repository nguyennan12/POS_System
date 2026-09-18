using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;

namespace POS.Application.UseCases.Employees.Commands.CreateEmployee;

public record CreateEmployeeCommand(string Name, string Username, string Password, string Pin, Guid RoleId, Guid? StoreId = null, bool IsChainOwner = false) : ICommand<EmployeeDto>, IRequirePermission
{
    public string RequiredPermission => "employees:create";
}
