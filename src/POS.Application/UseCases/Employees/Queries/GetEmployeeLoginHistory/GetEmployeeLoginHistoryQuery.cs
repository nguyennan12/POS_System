using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;

namespace POS.Application.UseCases.Employees.Queries.GetEmployeeLoginHistory;

public record GetEmployeeLoginHistoryQuery(Guid Id, int PageNumber = 1, int PageSize = 20) : IQuery<PagedEmployeeLoginHistory>, IRequirePermission
{
    public string RequiredPermission => "employees:read";
}
