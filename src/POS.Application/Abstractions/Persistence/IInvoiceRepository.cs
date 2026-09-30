using POS.Domain.Orders;
using POS.Contracts.V1.Invoices;

namespace POS.Application.Abstractions.Persistence;

public interface IInvoiceRepository
{
    Task<Invoice?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<(List<InvoiceSummaryResponse> Items, int TotalCount)> GetPagedAsync(
        Guid employeeId, Guid? storeId, bool isChainOwner,
        Guid? orderId, DateTimeOffset? from, DateTimeOffset? to,
        int pageNumber, int pageSize, CancellationToken cancellationToken = default);

    Task<bool> ExistsForOrderAsync(Guid orderId, CancellationToken cancellationToken = default);
    Task<long> GetNextSequenceAsync(Guid storeId, DateOnly date, CancellationToken cancellationToken = default);
    Task AddAsync(Invoice invoice, CancellationToken cancellationToken = default);
}
