using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;
using POS.Application.Abstractions.Reports;
using POS.Domain.Common;

namespace POS.Application.UseCases.Reports.Queries.GetExportReportFile;

public record ExportReportFileDto(
    string FileName,
    string ContentType,
    byte[] FileBytes);

public record GetExportReportFileQuery(Guid JobId) : IQuery<ExportReportFileDto>, IRequirePermission
{
    public string RequiredPermission => "reports:export";
}

public class GetExportReportFileQueryHandler : IQueryHandler<GetExportReportFileQuery, ExportReportFileDto>
{
    private readonly IReportExportJobStore _jobStore;

    public GetExportReportFileQueryHandler(IReportExportJobStore jobStore)
    {
        _jobStore = jobStore;
    }

    public Task<Result<ExportReportFileDto>> Handle(
        GetExportReportFileQuery query,
        CancellationToken cancellationToken)
    {
        var job = _jobStore.GetJob(query.JobId);
        if (job is null)
        {
            return Task.FromResult<Result<ExportReportFileDto>>(
                new Error(ErrorType.NotFound, "Export.JobNotFound", "Không tìm thấy yêu cầu xuất báo cáo."));
        }

        if (job.Status != "Completed" || job.FileBytes is null || string.IsNullOrWhiteSpace(job.FileName))
        {
            return Task.FromResult<Result<ExportReportFileDto>>(
                new Error(ErrorType.Invalid, "Export.NotReady", $"File chưa sẵn sàng để tải về. Trạng thái hiện tại: {job.Status}"));
        }

        return Task.FromResult(Result<ExportReportFileDto>.Success(
            new ExportReportFileDto(job.FileName, job.ContentType ?? "application/octet-stream", job.FileBytes)));
    }
}
