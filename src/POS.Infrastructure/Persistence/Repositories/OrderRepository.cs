using Microsoft.EntityFrameworkCore;
using POS.Application.Abstractions.Persistence;
using POS.Application.UseCases.Orders.DTOs;
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

    public async Task<(IReadOnlyList<OrderSummaryDto> Items, int TotalCount)> GetPagedAsync(
        Guid? storeId,
        Guid? shiftId,
        OrderStatus? status,
        DateTimeOffset? from,
        DateTimeOffset? to,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Orders.AsNoTracking();

        if (storeId.HasValue)
            query = query.Where(o => o.StoreId == storeId.Value);

        if (shiftId.HasValue)
            query = query.Where(o => o.ShiftId == shiftId.Value);

        if (status.HasValue)
            query = query.Where(o => o.Status == status.Value);

        if (from.HasValue)
        {
            var fromUtc = from.Value.UtcDateTime;
            query = query.Where(o => o.CreatedAt >= fromUtc);
        }

        if (to.HasValue)
        {
            var toUtc = to.Value.UtcDateTime;
            query = query.Where(o => o.CreatedAt <= toUtc);
        }

        var total = await query.CountAsync(cancellationToken);
        var offset = ((long)pageNumber - 1) * pageSize;
        if (offset >= total) return (Array.Empty<OrderSummaryDto>(), total);

        var items = await query
            .OrderByDescending(o => o.CreatedAt)
            .ThenByDescending(o => o.Id)
            .Skip((int)offset)
            .Take(pageSize)
            .Select(o => new OrderSummaryDto(
                o.Id,
                o.StoreId,
                o.ShiftId,
                o.CustomerId,
                o.Customer != null ? o.Customer.Name : null,
                o.Customer != null ? o.Customer.Phone : null,
                o.Status.ToString(),
                o.CurrencyCode,
                o.GrandTotal,
                (int)o.Items.Sum(i => i.Qty),
                new DateTimeOffset(o.CreatedAt, TimeSpan.Zero),
                o.PaidAt.HasValue ? new DateTimeOffset(o.PaidAt.Value, TimeSpan.Zero) : null
            ))
            .ToListAsync(cancellationToken);

        return (items, total);
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
