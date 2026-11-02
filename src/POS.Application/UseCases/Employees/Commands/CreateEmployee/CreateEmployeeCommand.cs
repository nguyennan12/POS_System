using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;

namespace POS.Application.UseCases.Employees.Commands.CreateEmployee;

public record CreateEmployeeCommand(string Name, string Username, string Password, string Pin, Guid RoleId, Guid? StoreId = null, bool IsChainOwner = false,
    string? Phone = null, string? Email = null, string? CitizenId = null, DateOnly? HireDate = null, string? EmploymentType = null, decimal? HourlyWage = null) : ICommand<EmployeeDto>, IRequirePermission
{
    public string RequiredPermission => "employees:manage";
}
