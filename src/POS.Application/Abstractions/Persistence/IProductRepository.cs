using POS.Domain.Products;

namespace POS.Application.Abstractions.Persistence;

/// <summary>Kết quả một dòng sản phẩm trong danh sách phân trang, kèm số lượng SKU và tên danh mục.</summary>
public record ProductSummaryRow(Product Product, int SkuCount, string CategoryName);

public interface IProductRepository
{
    /// <summary>Lấy danh sách sản phẩm phân trang theo cửa hàng với bộ lọc, trả kèm SkuCount và CategoryName.</summary>
    Task<(List<ProductSummaryRow> Items, int TotalCount)> GetPagedAsync(
        Guid storeId,
        string? search,
        Guid? categoryId,
        string? status,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>Lấy sản phẩm theo ID (kèm Category và Skus).</summary>
    Task<Product?> GetByIdWithSkusAsync(Guid id, Guid storeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// [FIX-7 / BUG-8] Batch pre-fetch: nạp tất cả sản phẩm theo danh sách tên (case-insensitive)
    /// trong 1 query duy nhất. Dùng để tái sử dụng Product đã tồn tại trong DB thay vì tạo trùng lặp.
    /// Key trả về: Name.ToUpperInvariant() → Product entity.
    /// </summary>
    Task<Dictionary<string, Product>> GetByNamesAsync(
        IEnumerable<string> names,
        Guid storeId,
        CancellationToken cancellationToken = default);

    Task AddAsync(Product product, CancellationToken cancellationToken = default);
    void Update(Product product);
    void Remove(Product product);
}
