using POS.Domain.Inventory.Suppliers;

namespace POS.Application.Abstractions.Persistence;

public interface ISupplierRepository
{
    Task<Supplier?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<(List<Supplier> Items, int TotalCount)> GetPagedAsync(
        string? search,
        bool? isActive,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task AddAsync(Supplier supplier, CancellationToken cancellationToken = default);

    Task<List<SupplierPayment>> GetPaymentsBySupplierAsync(
        Guid supplierId,
        CancellationToken cancellationToken = default);

    Task AddPaymentAsync(SupplierPayment payment, CancellationToken cancellationToken = default);
}
