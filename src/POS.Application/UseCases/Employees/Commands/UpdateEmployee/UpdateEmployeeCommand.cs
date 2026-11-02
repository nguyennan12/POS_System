using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;

namespace POS.Application.UseCases.Employees.Commands.UpdateEmployee;

public record UpdateEmployeeCommand(Guid Id, string Name, Guid RoleId, Guid? StoreId = null, bool IsChainOwner = false,
    string? Phone = null, string? Email = null, string? CitizenId = null, DateOnly? HireDate = null, string? EmploymentType = null, decimal? HourlyWage = null) : ICommand<EmployeeDto>, IRequirePermission
{
    public string RequiredPermission => "employees:manage";
}
