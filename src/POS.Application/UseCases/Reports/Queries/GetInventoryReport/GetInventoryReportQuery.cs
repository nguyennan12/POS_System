using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;
using POS.Application.Abstractions.Persistence;
using POS.Contracts.V1.Reports;
using POS.Domain.Common;

namespace POS.Application.UseCases.Reports.Queries.GetInventoryReport;

public record GetInventoryReportQuery(
    Guid? StoreId = null,
    Guid? CategoryId = null
) : IQuery<InventoryReportResponse>, IRequirePermission
{
    public string RequiredPermission => "reports:inventory_view";
}

public class GetInventoryReportQueryHandler : IQueryHandler<GetInventoryReportQuery, InventoryReportResponse>
{
    private readonly IReportRepository _reportRepository;
    private readonly ICurrentUser _currentUser;
    private readonly IEmployeeRepository _employeeRepository;

    public GetInventoryReportQueryHandler(
        IReportRepository reportRepository,
        ICurrentUser currentUser,
        IEmployeeRepository employeeRepository)
    {
        _reportRepository = reportRepository;
        _currentUser = currentUser;
        _employeeRepository = employeeRepository;
    }

    public async Task<Result<InventoryReportResponse>> Handle(
        GetInventoryReportQuery query,
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

        var report = await _reportRepository.GetInventoryReportAsync(
            effectiveStoreId,
            query.CategoryId,
            cancellationToken);

        return Result<InventoryReportResponse>.Success(report);
    }
}
