using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;
using POS.Application.Abstractions.Persistence;
using POS.Contracts.V1.Reports;
using POS.Domain.Common;

namespace POS.Application.UseCases.Reports.Queries.GetDashboardSummary;

public record GetDashboardSummaryQuery(Guid? StoreId = null, int Days = 7) : IQuery<DashboardSummaryResponse>, IRequirePermission
{
    public string RequiredPermission => "reports:dashboard_view";
}

public class GetDashboardSummaryQueryHandler : IQueryHandler<GetDashboardSummaryQuery, DashboardSummaryResponse>
{
    private readonly IReportRepository _reportRepository;
    private readonly ICurrentUser _currentUser;
    private readonly IEmployeeRepository _employeeRepository;

    public GetDashboardSummaryQueryHandler(
        IReportRepository reportRepository,
        ICurrentUser currentUser,
        IEmployeeRepository employeeRepository)
    {
        _reportRepository = reportRepository;
        _currentUser = currentUser;
        _employeeRepository = employeeRepository;
    }

    public async Task<Result<DashboardSummaryResponse>> Handle(
        GetDashboardSummaryQuery query,
        CancellationToken cancellationToken)
    {
        var effectiveStoreId = query.StoreId;

        if (_currentUser.EmployeeId.HasValue)
        {
            var employee = await _employeeRepository.GetByIdAsync(_currentUser.EmployeeId.Value, cancellationToken);
            if (employee != null && !employee.IsChainOwner)
            {
                // Non-chain owners can only view their assigned store
                effectiveStoreId = employee.StoreId;
            }
        }

        var summary = await _reportRepository.GetDashboardSummaryAsync(effectiveStoreId, query.Days, cancellationToken);
        return Result<DashboardSummaryResponse>.Success(summary);
    }
}
