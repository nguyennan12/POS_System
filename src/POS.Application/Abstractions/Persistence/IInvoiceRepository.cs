using POS.Domain.Orders;

namespace POS.Application.Abstractions.Persistence;

public interface IInvoiceRepository
{
    Task<bool> ExistsForOrderAsync(Guid orderId, CancellationToken cancellationToken = default);
    Task<long> GetNextSequenceAsync(Guid storeId, DateOnly date, CancellationToken cancellationToken = default);
    Task AddAsync(Invoice invoice, CancellationToken cancellationToken = default);
}
