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

    public async Task<int> GetNextSequenceAsync(Guid storeId, DateTime date, CancellationToken cancellationToken = default)
    {
        var dateStart = date.Date;
        var dateEnd = dateStart.AddDays(1);
        return await dbContext.Invoices
            .Where(i => i.Order.StoreId == storeId && i.IssuedAt >= dateStart && i.IssuedAt < dateEnd)
            .CountAsync(cancellationToken) + 1;
    }

    public async Task AddAsync(Invoice invoice, CancellationToken cancellationToken = default)
    {
        await dbContext.Invoices.AddAsync(invoice, cancellationToken);
    }
}
