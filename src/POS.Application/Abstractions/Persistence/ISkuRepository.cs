using POS.Domain.Products;

namespace POS.Application.Abstractions.Persistence;

public interface ISkuRepository
{
    Task<Sku?> GetByIdWithProductAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Sku?> GetByBarcodeWithProductAsync(string barcode, Guid storeId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Sku>> GetByIdsWithProductAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);
    Task<bool> IsSkuCodeUniqueAsync(string skuCode, Guid storeId, Guid? excludeId = null, CancellationToken cancellationToken = default);
    Task<bool> IsBarcodeUniqueAsync(string barcode, Guid storeId, Guid? excludeId = null, CancellationToken cancellationToken = default);
    Task AddAsync(Sku sku, CancellationToken cancellationToken = default);
}
