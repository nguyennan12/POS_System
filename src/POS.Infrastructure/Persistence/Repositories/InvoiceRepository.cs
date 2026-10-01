using Microsoft.EntityFrameworkCore;
using POS.Application.Abstractions.Persistence;
using POS.Domain.Orders;
using POS.Application.UseCases.Invoices.Mappings;
using POS.Contracts.V1.Invoices;

namespace POS.Infrastructure.Persistence.Repositories;

public class InvoiceRepository(AppDbContext dbContext) : IInvoiceRepository
{
    public Task<Invoice?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.Invoices.AsNoTracking()
            .Include(i => i.Order).ThenInclude(o => o.Items)
                .ThenInclude(i => i.Sku).ThenInclude(s => s.Product)
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);

    public async Task<(List<InvoiceSummaryResponse> Items, int TotalCount)> GetPagedAsync(
        Guid employeeId, Guid? storeId, bool isChainOwner,
        Guid? orderId, DateTimeOffset? from, DateTimeOffset? to,
        int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Invoices.AsNoTracking();
        // Apply the same scope as GetPaymentStatus before counting/paging, so neither
        // rows nor TotalCount disclose invoices from inaccessible stores.
        if (!isChainOwner)
            query = query.Where(i => i.Order.StoreId == storeId);
        if (orderId.HasValue) query = query.Where(i => i.OrderId == orderId.Value);
        if (from.HasValue)
        {
            var fromUtc = from.Value.UtcDateTime;
            query = query.Where(i => i.IssuedAt >= fromUtc);
        }
        if (to.HasValue)
        {
            var toUtc = to.Value.UtcDateTime;
            query = query.Where(i => i.IssuedAt <= toUtc);
        }

        var total = await query.CountAsync(cancellationToken);
        var offset = ((long)pageNumber - 1) * pageSize;
        if (offset >= total) return ([], total);
        var items = await query.OrderByDescending(i => i.IssuedAt).ThenByDescending(i => i.Id)
            .Skip((int)offset).Take(pageSize)
            .Select(InvoiceMappings.SummaryProjection)
            .ToListAsync(cancellationToken);
        return (items, total);
    }

    public async Task<bool> ExistsForOrderAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        return dbContext.Invoices.Local.Any(i => i.OrderId == orderId) || await dbContext.Invoices
            .AnyAsync(i => i.OrderId == orderId, cancellationToken);
    }

    public async Task<long> GetNextSequenceAsync(Guid storeId, DateOnly date, CancellationToken cancellationToken = default)
    {
        // The UPSERT locks this store/day row until the UnitOfWork commits.
        // Serializable conflicts must replay the whole checkout decision.
        var values = await dbContext.Database.SqlQuery<long>($"""
            INSERT INTO invoice_sequences (store_id, invoice_date, last_value)
            VALUES ({storeId}, {date}, 1)
            ON CONFLICT (store_id, invoice_date)
            DO UPDATE SET last_value = invoice_sequences.last_value + 1
            RETURNING last_value AS "Value"
            """).ToListAsync(cancellationToken);
        // Materialize before Single: INSERT ... RETURNING is not composable SQL.
        return values.Single();
    }

    public async Task AddAsync(Invoice invoice, CancellationToken cancellationToken = default)
    {
        await dbContext.Invoices.AddAsync(invoice, cancellationToken);
    }
}
