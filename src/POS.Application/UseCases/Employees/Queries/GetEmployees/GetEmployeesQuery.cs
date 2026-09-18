using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;

namespace POS.Application.UseCases.Employees.Queries.GetEmployees;

public record GetEmployeesQuery(Guid? StoreId = null, Guid? RoleId = null, string? Search = null, bool? IsActive = null, int PageNumber = 1, int PageSize = 20) : IQuery<PagedEmployees>, IRequirePermission
{
    public string RequiredPermission => "employees:read";
}
