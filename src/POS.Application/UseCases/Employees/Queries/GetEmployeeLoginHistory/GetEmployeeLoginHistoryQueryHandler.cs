using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;
using POS.Application.Abstractions.Persistence;
using POS.Domain.Common;

namespace POS.Application.UseCases.Employees.Queries.GetEmployeeLoginHistory;

public sealed class GetEmployeeLoginHistoryQueryHandler(IEmployeeRepository employees, IAuditLogRepository auditLogs,
    ICurrentUser currentUser) : IQueryHandler<GetEmployeeLoginHistoryQuery, PagedEmployeeLoginHistory>
{
    public async Task<Result<PagedEmployeeLoginHistory>> Handle(GetEmployeeLoginHistoryQuery query, CancellationToken cancellationToken)
    {
        var caller = await employees.GetDetailAsync(currentUser.EmployeeId!.Value, cancellationToken);
        if (caller is null || (EmployeeAccess.Level(caller) <= 1 && query.Id != caller.Id))
            return EmployeeErrors.Forbidden;
        var target = await employees.GetDetailAsync(query.Id, cancellationToken);
        if (target is null) return EmployeeErrors.NotFound;
        if (!EmployeeAccess.CanRead(caller, target)) return EmployeeErrors.Forbidden;
        var (items, total) = await auditLogs.GetLoginHistoryAsync(query.Id, query.PageNumber, query.PageSize, cancellationToken);
        return new PagedEmployeeLoginHistory(items.Select(a => a.ToDto()).ToList(), total, query.PageNumber, query.PageSize);
    }
}
