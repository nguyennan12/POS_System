using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;
using POS.Application.Abstractions.Persistence;
using POS.Contracts.V1.Reports;
using POS.Domain.Common;

namespace POS.Application.UseCases.Reports.Queries.GetShiftReport;

public record GetShiftReportQuery(Guid ShiftId) : IQuery<ShiftReportResponse>, IRequirePermission
{
    public string RequiredPermission => "reports:revenue_view";
}

public class GetShiftReportQueryHandler : IQueryHandler<GetShiftReportQuery, ShiftReportResponse>
{
    private readonly IReportRepository _reportRepository;

    public GetShiftReportQueryHandler(IReportRepository reportRepository)
    {
        _reportRepository = reportRepository;
    }

    public async Task<Result<ShiftReportResponse>> Handle(
        GetShiftReportQuery query,
        CancellationToken cancellationToken)
    {
        var report = await _reportRepository.GetShiftReportAsync(query.ShiftId, cancellationToken);
        if (report is null)
        {
            return new Error(ErrorType.NotFound, "Shift.NotFound", "Không tìm thấy thông tin ca làm việc.");
        }

        return Result<ShiftReportResponse>.Success(report);
    }
}
