using POS.Domain.Auditing;

namespace POS.Application.Abstractions.Persistence;

public interface IAuditLogRepository
{
    Task AddAsync(AuditLog auditLog, CancellationToken cancellationToken = default);
    Task<(List<AuditLog> Items, int TotalCount)> GetLoginHistoryAsync(Guid employeeId,
        int pageNumber, int pageSize, CancellationToken cancellationToken = default);
}
