using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;
using POS.Application.Abstractions.Persistence;
using POS.Contracts.V1.Reports;
using POS.Domain.Common;

namespace POS.Application.UseCases.Reports.Queries.GetRevenueReport;

public record GetRevenueReportQuery(
    DateTimeOffset? From = null,
    DateTimeOffset? To = null,
    Guid? StoreId = null,
    Guid? EmployeeId = null
) : IQuery<RevenueReportResponse>, IRequirePermission
{
    public string RequiredPermission => "reports:revenue_view";
}

public class GetRevenueReportQueryHandler : IQueryHandler<GetRevenueReportQuery, RevenueReportResponse>
{
    private readonly IReportRepository _reportRepository;
    private readonly ICurrentUser _currentUser;
    private readonly IEmployeeRepository _employeeRepository;

    public GetRevenueReportQueryHandler(
        IReportRepository reportRepository,
        ICurrentUser currentUser,
        IEmployeeRepository employeeRepository)
    {
        _reportRepository = reportRepository;
        _currentUser = currentUser;
        _employeeRepository = employeeRepository;
    }

    public async Task<Result<RevenueReportResponse>> Handle(
        GetRevenueReportQuery query,
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

        var report = await _reportRepository.GetRevenueReportAsync(
            query.From,
            query.To,
            effectiveStoreId,
            query.EmployeeId,
            cancellationToken);

        return Result<RevenueReportResponse>.Success(report);
    }
}
