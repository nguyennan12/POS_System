using Microsoft.EntityFrameworkCore;
using POS.Application.Abstractions.Persistence;
using POS.Domain.Orders;
using POS.Domain.Orders.Enums;

namespace POS.Infrastructure.Persistence.Repositories;

public class PaymentRepository(AppDbContext dbContext) : IPaymentRepository
{
    public Task<Payment?> GetForWebhookAsync(PaymentMethod method, string merchantReference, CancellationToken ct) =>
        dbContext.Payments.IgnoreQueryFilters()
            .Include(p => p.Order).ThenInclude(o => o.Payments)
            .FirstOrDefaultAsync(p => p.Method == method && p.MerchantReference == merchantReference, ct);

    public async Task<Payment?> GetTrackedAsync(Guid id, CancellationToken ct)
    {
        // Generation reserves in one transaction and stores provider metadata in another.
        // Refresh the reserved entity so a concurrently completed webhook cannot be overwritten.
        var local = dbContext.Payments.Local.FirstOrDefault(p => p.Id == id);
        if (local is not null) await dbContext.Entry(local).ReloadAsync(ct);
        return await dbContext.Payments.Include(p => p.Order).FirstOrDefaultAsync(p => p.Id == id, ct);
    }

    public Task<bool> TransactionExistsAsync(PaymentMethod method, string transactionReference, Guid exceptPaymentId, CancellationToken ct) =>
        dbContext.Payments.IgnoreQueryFilters().AnyAsync(p => p.Method == method && p.TransactionRef == transactionReference
            && p.Id != exceptPaymentId, ct);

    public Task<Payment?> GetByIdWithOrderAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.Payments.AsNoTracking().Include(payment => payment.Order)
            .FirstOrDefaultAsync(payment => payment.Id == id, cancellationToken);
}
