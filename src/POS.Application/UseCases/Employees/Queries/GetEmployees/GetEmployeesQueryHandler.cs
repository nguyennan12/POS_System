using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;
using POS.Application.Abstractions.Persistence;
using POS.Domain.Common;

namespace POS.Application.UseCases.Employees.Queries.GetEmployees;

public sealed class GetEmployeesQueryHandler(IEmployeeRepository employees, ICurrentUser currentUser)
    : IQueryHandler<GetEmployeesQuery, PagedEmployees>
{
    public async Task<Result<PagedEmployees>> Handle(GetEmployeesQuery query, CancellationToken cancellationToken)
    {
        var caller = await employees.GetDetailAsync(currentUser.EmployeeId!.Value, cancellationToken);
        if (caller is null) return EmployeeErrors.Forbidden;
        var ownOnly = EmployeeAccess.Level(caller) <= 1;
        if (!caller.IsChainOwner && (caller.StoreId == null ||
            (query.StoreId.HasValue && query.StoreId != caller.StoreId)))
            return EmployeeErrors.Forbidden;
        var (items, total) = await employees.GetPagedAsync(caller.IsChainOwner ? query.StoreId : caller.StoreId,
            ownOnly ? caller.Id : null, query.RoleId, query.Search, query.IsActive,
            query.PageNumber, query.PageSize, cancellationToken);
        return new PagedEmployees(items.Select(e => e.ToDto()).ToList(), total, query.PageNumber, query.PageSize);
    }
}
