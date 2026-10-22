using POS.Domain.Orders;
using POS.Domain.Orders.Enums;

namespace POS.Application.Abstractions.Persistence;

public interface IPaymentRepository
{
    Task<Payment?> GetByIdWithOrderAsync(Guid id, CancellationToken cancellationToken = default);
    // Webhooks have no tenant context; implementations must bypass store query filters.
    Task<Payment?> GetForWebhookAsync(PaymentMethod method, string merchantReference, CancellationToken ct);
    Task<Payment?> GetTrackedAsync(Guid id, CancellationToken ct);
    Task<bool> TransactionExistsAsync(PaymentMethod method, string transactionReference, Guid exceptPaymentId, CancellationToken ct);
}
