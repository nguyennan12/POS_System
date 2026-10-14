using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;
using POS.Application.Abstractions.Reports;
using POS.Contracts.V1.Reports;
using POS.Domain.Common;

namespace POS.Application.UseCases.Reports.Queries.GetExportJobStatus;

public record GetExportJobStatusQuery(Guid JobId) : IQuery<ExportJobStatusResponse>, IRequirePermission
{
    public string RequiredPermission => "reports:export";
}

public class GetExportJobStatusQueryHandler : IQueryHandler<GetExportJobStatusQuery, ExportJobStatusResponse>
{
    private readonly IReportExportJobStore _jobStore;

    public GetExportJobStatusQueryHandler(IReportExportJobStore jobStore)
    {
        _jobStore = jobStore;
    }

    public Task<Result<ExportJobStatusResponse>> Handle(
        GetExportJobStatusQuery query,
        CancellationToken cancellationToken)
    {
        var job = _jobStore.GetJob(query.JobId);
        if (job is null)
        {
            return Task.FromResult<Result<ExportJobStatusResponse>>(
                new Error(ErrorType.NotFound, "Export.JobNotFound", "Không tìm thấy yêu cầu xuất báo cáo."));
        }

        return Task.FromResult(Result<ExportJobStatusResponse>.Success(job.ToStatusResponse()));
    }
}
