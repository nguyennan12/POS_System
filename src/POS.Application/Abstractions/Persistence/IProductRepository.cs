using POS.Domain.Products;

namespace POS.Application.Abstractions.Persistence;

/// <summary>Kết quả một dòng sản phẩm trong danh sách phân trang, kèm số lượng SKU.</summary>
public record ProductSummaryRow(Product Product, int SkuCount);

public interface IProductRepository
{
    /// <summary>Lấy danh sách sản phẩm phân trang theo cửa hàng với bộ lọc, trả kèm SkuCount.</summary>
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

    Task AddAsync(Product product, CancellationToken cancellationToken = default);
    void Update(Product product);
    void Remove(Product product);
}
