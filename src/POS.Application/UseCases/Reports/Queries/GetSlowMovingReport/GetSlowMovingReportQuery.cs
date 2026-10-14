using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;
using POS.Application.Abstractions.Persistence;
using POS.Contracts.V1.Reports;
using POS.Domain.Common;

namespace POS.Application.UseCases.Reports.Queries.GetSlowMovingReport;

public record GetSlowMovingReportQuery(
    int Days = 30,
    Guid? StoreId = null
) : IQuery<IReadOnlyList<SlowMovingItemResponse>>, IRequirePermission
{
    public string RequiredPermission => "reports:inventory_view";
}

public class GetSlowMovingReportQueryHandler : IQueryHandler<GetSlowMovingReportQuery, IReadOnlyList<SlowMovingItemResponse>>
{
    private readonly IReportRepository _reportRepository;
    private readonly ICurrentUser _currentUser;
    private readonly IEmployeeRepository _employeeRepository;

    public GetSlowMovingReportQueryHandler(
        IReportRepository reportRepository,
        ICurrentUser currentUser,
        IEmployeeRepository employeeRepository)
    {
        _reportRepository = reportRepository;
        _currentUser = currentUser;
        _employeeRepository = employeeRepository;
    }

    public async Task<Result<IReadOnlyList<SlowMovingItemResponse>>> Handle(
        GetSlowMovingReportQuery query,
        CancellationToken cancellationToken)
    {
        var effectiveStoreId = query.StoreId;

        if (_currentUser.EmployeeId.HasValue)
        {
            var employee = await _employeeRepository.GetByIdAsync(_currentUser.EmployeeId.Value, cancellationToken);
            if (employee != null && !employee.IsChainOwner)
            {
                effectiveStoreId = employee.StoreId;
            }
        }

        var result = await _reportRepository.GetSlowMovingItemsAsync(
            query.Days,
            effectiveStoreId,
            cancellationToken);

        return Result<IReadOnlyList<SlowMovingItemResponse>>.Success(result);
    }
}
