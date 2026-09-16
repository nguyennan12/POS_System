using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;

namespace POS.Application.UseCases.Employees.Queries.GetEmployeeDetail;

public record GetEmployeeDetailQuery(Guid Id) : IQuery<EmployeeDto>, IRequirePermission
{
    public string RequiredPermission => "employees:read";
}
