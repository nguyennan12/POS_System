using POS.Contracts.V1.Reports;

namespace POS.Application.Abstractions.Reports;

public record ReportExportJob(
    Guid JobId,
    string Status,
    DateTimeOffset CreatedAt,
    string? FileName = null,
    string? ContentType = null,
    byte[]? FileBytes = null,
    string? ErrorMessage = null
)
{
    public ExportJobStatusResponse ToStatusResponse() =>
        new(
            JobId,
            Status,
            Status == "Completed" ? $"/api/v1/reports/export/{JobId}/download" : null,
            ErrorMessage);
}

public interface IReportExportJobStore
{
    void CreateJob(Guid jobId);
    void CompleteJob(Guid jobId, string fileName, string contentType, byte[] fileBytes);
    void FailJob(Guid jobId, string errorMessage);
    ReportExportJob? GetJob(Guid jobId);
}
