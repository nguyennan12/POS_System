using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;
using POS.Application.Abstractions.Persistence;
using POS.Domain.Common;

namespace POS.Application.UseCases.Employees.Queries.GetEmployeeDetail;

public sealed class GetEmployeeDetailQueryHandler(IEmployeeRepository employees, ICurrentUser currentUser)
    : IQueryHandler<GetEmployeeDetailQuery, EmployeeDto>
{
    public async Task<Result<EmployeeDto>> Handle(GetEmployeeDetailQuery query, CancellationToken cancellationToken)
    {
        var caller = await employees.GetDetailAsync(currentUser.EmployeeId!.Value, cancellationToken);
        if (caller is null || (EmployeeAccess.Level(caller) <= 1 && query.Id != caller.Id))
            return EmployeeErrors.Forbidden;
        var target = await employees.GetDetailAsync(query.Id, cancellationToken);
        if (target is null) return EmployeeErrors.NotFound;
        if (!EmployeeAccess.CanRead(caller, target)) return EmployeeErrors.Forbidden;
        return target.ToDto();
    }
}
