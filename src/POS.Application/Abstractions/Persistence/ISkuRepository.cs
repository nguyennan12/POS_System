using POS.Domain.Products;

namespace POS.Application.Abstractions.Persistence;

public interface ISkuRepository
{
    Task<Sku?> GetByIdWithProductAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Sku?> GetByBarcodeWithProductAsync(string barcode, Guid storeId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Sku>> GetByIdsWithProductAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);
}
