using POS.Domain.Products;

namespace POS.Application.Abstractions.Persistence;

public interface ISkuRepository
{
    Task<Sku?> GetByIdWithProductAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Sku?> GetByBarcodeWithProductAsync(string barcode, Guid storeId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Sku>> GetByIdsWithProductAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);

    /// <summary>
    /// Batch pre-fetch: nạp tất cả SKU theo danh sách SkuCode trong 1 query duy nhất.
    /// Dùng để xật định SKU nào đã tồn tại (Upsert) trong bulk-import.
    /// </summary>
    Task<Dictionary<string, Sku>> GetBySkuCodesAsync(
        IEnumerable<string> skuCodes,
        Guid storeId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Batch pre-fetch: nạp tất cả SKU theo danh sách Barcode trong 1 query duy nhất.
    /// Dùng để kiểm tra Barcode đã tồn tại trong bulk-import.
    /// </summary>
    Task<HashSet<string>> GetExistingBarcodesAsync(
        IEnumerable<string> barcodes,
        Guid storeId,
        CancellationToken cancellationToken = default);

    Task<bool> IsSkuCodeUniqueAsync(string skuCode, Guid storeId, Guid? excludeId = null, CancellationToken cancellationToken = default);
    Task<bool> IsBarcodeUniqueAsync(string barcode, Guid storeId, Guid? excludeId = null, CancellationToken cancellationToken = default);
    Task AddAsync(Sku sku, CancellationToken cancellationToken = default);
    void Remove(Sku sku);
}
