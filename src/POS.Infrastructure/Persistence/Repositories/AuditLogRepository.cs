using Microsoft.EntityFrameworkCore;
using POS.Application.Abstractions.Persistence;
using POS.Domain.Auditing;

namespace POS.Infrastructure.Persistence.Repositories;

public sealed class AuditLogRepository(AppDbContext context) : IAuditLogRepository
{
    public async Task AddAsync(AuditLog auditLog, CancellationToken cancellationToken = default) =>
        await context.AuditLogs.AddAsync(auditLog, cancellationToken);

    public async Task<(List<AuditLog> Items, int TotalCount)> GetLoginHistoryAsync(Guid employeeId,
        int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = context.AuditLogs.AsNoTracking().Where(a => a.EmployeeId == employeeId && a.EntityType == "Employee" &&
            (a.Action == AuthenticationAuditActions.Login || a.Action == AuthenticationAuditActions.LoginFailed ||
             a.Action == AuthenticationAuditActions.Logout));
        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(a => a.CreatedAt).ThenByDescending(a => a.Id)
            .Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return (items, total);
    }
}
