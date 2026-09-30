using Microsoft.EntityFrameworkCore;
using POS.Application.Abstractions.Persistence;
using POS.Domain.Orders;

namespace POS.Infrastructure.Persistence.Repositories;

public class InvoiceRepository(AppDbContext dbContext) : IInvoiceRepository
{
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
