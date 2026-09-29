using System.Net.Http;
using POS.Contracts.V1.Common;
using POS.Contracts.V1.Products;
using POS.WinUI.Constants;
using POS.WinUI.Services;

namespace POS.WinUI.ApiClients;

public sealed class ProductApiClient : BaseApiClient
{
    public ProductApiClient(HttpClient http, SessionService session)
        : base(http, session) { }

    /// <summary>Lấy danh sách sản phẩm phân trang với bộ lọc.</summary>
    public Task<ApiResponse<PagedResponse<ProductSummaryResponse>>?> GetProductsAsync(
        ProductFilterRequest filter, CancellationToken ct = default)
    {
        var qs = BuildQueryString(filter);
        return GetAsync<ApiResponse<PagedResponse<ProductSummaryResponse>>>(
            $"{ApiRoutes.Products.Base}?{qs}", ct);
    }

    /// <summary>Lấy chi tiết sản phẩm theo ID (kèm danh sách SKU).</summary>
    public Task<ApiResponse<ProductDetailResponse>?> GetProductByIdAsync(
        Guid id, CancellationToken ct = default) =>
        GetAsync<ApiResponse<ProductDetailResponse>>(ApiRoutes.Products.ById(id), ct);

    /// <summary>Tạo sản phẩm mới.</summary>
    public Task<ApiResponse<ProductDetailResponse>?> CreateProductAsync(
        CreateProductRequest request, CancellationToken ct = default) =>
        PostAsync<ApiResponse<ProductDetailResponse>>(ApiRoutes.Products.Base, request, ct);

    /// <summary>Cập nhật thông tin sản phẩm.</summary>
    public Task<ApiResponse<ProductDetailResponse>?> UpdateProductAsync(
        Guid id, UpdateProductRequest request, CancellationToken ct = default) =>
        PutAsync<ApiResponse<ProductDetailResponse>>(ApiRoutes.Products.ById(id), request, ct);

    /// <summary>Tra cứu SKU theo mã vạch.</summary>
    public Task<ApiResponse<SkuBarcodeLookupResponse>?> LookupBarcodeAsync(
        string code, CancellationToken ct = default) =>
        GetAsync<ApiResponse<SkuBarcodeLookupResponse>>(ApiRoutes.Skus.ByBarcode(code), ct);

    // ── Helper ────────────────────────────────────────────────────────────────

    private static string BuildQueryString(ProductFilterRequest f)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(f.Search))
            parts.Add($"search={Uri.EscapeDataString(f.Search)}");
        if (f.CategoryId.HasValue)
            parts.Add($"categoryId={f.CategoryId}");
        if (!string.IsNullOrWhiteSpace(f.Status))
            parts.Add($"status={f.Status}");
        parts.Add($"pageNumber={f.PageNumber}");
        parts.Add($"pageSize={f.PageSize}");
        return string.Join("&", parts);
    }
}
