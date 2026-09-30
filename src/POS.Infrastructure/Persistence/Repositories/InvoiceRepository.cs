using Microsoft.EntityFrameworkCore;
using POS.Application.Abstractions.Persistence;
using POS.Domain.Orders;

namespace POS.Infrastructure.Persistence.Repositories;

public class InvoiceRepository(AppDbContext dbContext) : IInvoiceRepository
{
    public async Task<bool> ExistsForOrderAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Invoices
            .AnyAsync(i => i.OrderId == orderId, cancellationToken);
    }

    public async Task<long> GetNextSequenceAsync(Guid storeId, DateOnly date, CancellationToken cancellationToken = default)
    {
        // The UPSERT locks this store/day row until the UnitOfWork commits.
        // Serializable conflicts must replay the whole checkout decision.
        await dbContext.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO invoice_sequences (store_id, invoice_date, last_value)
            VALUES ({storeId}, {date}, 1)
            ON CONFLICT (store_id, invoice_date)
            DO UPDATE SET last_value = invoice_sequences.last_value + 1
            """, cancellationToken);
        return await dbContext.InvoiceSequences.AsNoTracking()
            .Where(s => s.StoreId == storeId && s.InvoiceDate == date)
            .Select(s => s.LastValue)
            .SingleAsync(cancellationToken);
    }

    public async Task AddAsync(Invoice invoice, CancellationToken cancellationToken = default)
    {
        await dbContext.Invoices.AddAsync(invoice, cancellationToken);
    }
}
