using Microsoft.EntityFrameworkCore;
using POS.Application.Abstractions.Persistence;
using POS.Domain.Orders;
using POS.Domain.Orders.Enums;

namespace POS.Infrastructure.Persistence.Repositories;

public class OrderRepository(AppDbContext dbContext) : IOrderRepository
{
    public void ReplaceDiscounts(IEnumerable<OrderDiscount> previous, IEnumerable<OrderDiscount> current)
    {
        // Recalculation creates new entities with client-generated UUIDs.
        // Explicitly insert them rather than letting EF infer an update by key.
        dbContext.OrderDiscounts.RemoveRange(previous);
        dbContext.OrderDiscounts.AddRange(current);
    }

    public Task AddPaymentsAsync(IEnumerable<Payment> payments, CancellationToken cancellationToken = default) =>
        dbContext.Payments.AddRangeAsync(payments, cancellationToken);

    public Task<bool> PaymentReferenceExistsAsync(PaymentMethod method, string transactionRef, CancellationToken cancellationToken = default) =>
        dbContext.Payments.AnyAsync(p => p.Method == method && p.TransactionRef == transactionRef, cancellationToken);

    public async Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await dbContext.Orders
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
    }

    public async Task<Order?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await dbContext.Orders
            .Include(o => o.Store)
            .Include(o => o.Customer)
            .Include(o => o.CreatedByEmployee)
            .Include(o => o.Items)
                .ThenInclude(i => i.Sku)
                    .ThenInclude(s => s.Product)
            .Include(o => o.Discounts)
            .Include(o => o.Payments)
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
    }

    public async Task AddAsync(Order order, CancellationToken cancellationToken = default)
    {
        await dbContext.Orders.AddAsync(order, cancellationToken);
    }

    public void Update(Order order)
    {
        dbContext.Orders.Update(order);
    }
}
