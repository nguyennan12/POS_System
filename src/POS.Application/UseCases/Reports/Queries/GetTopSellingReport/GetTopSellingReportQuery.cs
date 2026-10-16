using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;
using POS.Application.Abstractions.Persistence;
using POS.Contracts.V1.Reports;
using POS.Domain.Common;

namespace POS.Application.UseCases.Reports.Queries.GetTopSellingReport;

public record GetTopSellingReportQuery(
    DateTimeOffset? From = null,
    DateTimeOffset? To = null,
    int Limit = 5,
    Guid? StoreId = null
) : IQuery<IReadOnlyList<TopSellingProductItem>>, IRequirePermission
{
    public string RequiredPermission => "reports:revenue_view";
}

public class GetTopSellingReportQueryHandler : IQueryHandler<GetTopSellingReportQuery, IReadOnlyList<TopSellingProductItem>>
{
    private readonly IReportRepository _reportRepository;
    private readonly ICurrentUser _currentUser;
    private readonly IEmployeeRepository _employeeRepository;

    public GetTopSellingReportQueryHandler(
        IReportRepository reportRepository,
        ICurrentUser currentUser,
        IEmployeeRepository employeeRepository)
    {
        _reportRepository = reportRepository;
        _currentUser = currentUser;
        _employeeRepository = employeeRepository;
    }

    public async Task<Result<IReadOnlyList<TopSellingProductItem>>> Handle(
        GetTopSellingReportQuery query,
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

        var result = await _reportRepository.GetTopSellingAsync(
            query.From,
            query.To,
            query.Limit,
            effectiveStoreId,
            cancellationToken);

        return Result<IReadOnlyList<TopSellingProductItem>>.Success(result);
    }
}
