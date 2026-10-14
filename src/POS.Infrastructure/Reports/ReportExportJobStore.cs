using System.Collections.Concurrent;
using POS.Application.Abstractions.Reports;

namespace POS.Infrastructure.Reports;

public class ReportExportJobStore : IReportExportJobStore
{
    private readonly ConcurrentDictionary<Guid, ReportExportJob> _jobs = new();

    public void CreateJob(Guid jobId)
    {
        _jobs[jobId] = new ReportExportJob(
            JobId: jobId,
            Status: "Processing",
            CreatedAt: DateTimeOffset.UtcNow);
    }

    public void CompleteJob(Guid jobId, string fileName, string contentType, byte[] fileBytes)
    {
        if (_jobs.TryGetValue(jobId, out var job))
        {
            _jobs[jobId] = job with
            {
                Status = "Completed",
                FileName = fileName,
                ContentType = contentType,
                FileBytes = fileBytes
            };
        }
    }

    public void FailJob(Guid jobId, string errorMessage)
    {
        if (_jobs.TryGetValue(jobId, out var job))
        {
            _jobs[jobId] = job with
            {
                Status = "Failed",
                ErrorMessage = errorMessage
            };
        }
    }

    public ReportExportJob? GetJob(Guid jobId)
    {
        _jobs.TryGetValue(jobId, out var job);
        return job;
    }
}
