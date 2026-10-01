using POS.Domain.Orders;
using POS.Domain.Orders.Enums;

namespace POS.Application.Abstractions.Persistence;

public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Order?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> PaymentReferenceExistsAsync(PaymentMethod method, string transactionRef, CancellationToken cancellationToken = default);
    Task AddAsync(Order order, CancellationToken cancellationToken = default);
    Task AddPaymentsAsync(IEnumerable<Payment> payments, CancellationToken cancellationToken = default);
    void ReplaceDiscounts(IEnumerable<OrderDiscount> previous, IEnumerable<OrderDiscount> current);
    void Update(Order order);
}
