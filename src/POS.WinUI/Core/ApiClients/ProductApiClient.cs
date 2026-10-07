using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using POS.Contracts.V1.Common;
using POS.Contracts.V1.Products;
using POS.WinUI.Core.Constants;
using POS.WinUI.Core.Services;

namespace POS.WinUI.Core.ApiClients;

/// <summary>
/// Client gọi REST API cho sản phẩm và SKU hàng hóa (Products & Skus)
/// </summary>
public sealed class ProductApiClient : BaseApiClient
{
    public ProductApiClient(HttpClient http, SessionService session)
        : base(http, session) { }

    /// <summary>
    /// Lấy danh mục sản phẩm tối ưu cho quầy bán hàng POS (kèm SKU, giá bán, tồn kho, mã vạch trong 1 request duy nhất)
    /// </summary>
    public Task<ApiResponse<PagedResponse<PosCatalogItemResponse>>?> GetPosCatalogAsync(
        Guid? categoryId = null,
        string? search = null,
        int pageNumber = 1,
        int pageSize = 100,
        CancellationToken ct = default)
    {
        var queryParams = new List<string>
        {
            $"pageNumber={pageNumber}",
            $"pageSize={pageSize}"
        };

        if (categoryId.HasValue)
            queryParams.Add($"categoryId={categoryId.Value}");

        if (!string.IsNullOrWhiteSpace(search))
            queryParams.Add($"search={Uri.EscapeDataString(search.Trim())}");

        var url = $"{ApiRoutes.Products.PosCatalog}?{string.Join("&", queryParams)}";
        return GetAsync<ApiResponse<PagedResponse<PosCatalogItemResponse>>>(url, ct);
    }

    /// <summary>
    /// Lấy danh sách sản phẩm phân trang kèm bộ lọc danh mục và từ khóa tìm kiếm
    /// </summary>
    public Task<ApiResponse<PagedResponse<ProductSummaryResponse>>?> GetProductsAsync(
        Guid? categoryId = null,
        string? search = null,
        string? status = null,
        int pageNumber = 1,
        int pageSize = 50,
        CancellationToken ct = default)
    {
        var queryParams = new List<string>
        {
            $"pageNumber={pageNumber}",
            $"pageSize={pageSize}"
        };

        if (categoryId.HasValue)
            queryParams.Add($"categoryId={categoryId.Value}");

        if (!string.IsNullOrWhiteSpace(search))
            queryParams.Add($"search={Uri.EscapeDataString(search.Trim())}");

        if (!string.IsNullOrWhiteSpace(status))
            queryParams.Add($"status={Uri.EscapeDataString(status.Trim())}");

        var url = $"{ApiRoutes.Products.Base}?{string.Join("&", queryParams)}";
        return GetAsync<ApiResponse<PagedResponse<ProductSummaryResponse>>>(url, ct);
    }

    /// <summary>
    /// Lấy thông tin chi tiết sản phẩm kèm danh sách SKU và tồn kho
    /// </summary>
    public Task<ApiResponse<ProductDetailResponse>?> GetProductByIdAsync(Guid id, CancellationToken ct = default)
        => GetAsync<ApiResponse<ProductDetailResponse>>(ApiRoutes.Products.GetById(id), ct);

    /// <summary>
    /// Tra cứu thông tin SKU theo mã vạch (dùng cho máy quét POS)
    /// </summary>
    public Task<ApiResponse<SkuBarcodeLookupResponse>?> GetSkuByBarcodeAsync(string barcode, CancellationToken ct = default)
        => GetAsync<ApiResponse<SkuBarcodeLookupResponse>>(ApiRoutes.Skus.GetByBarcode(barcode), ct);

    /// <summary>
    /// Lấy chi tiết SKU theo ID
    /// </summary>
    public Task<ApiResponse<SkuDetailResponse>?> GetSkuByIdAsync(Guid skuId, CancellationToken ct = default)
        => GetAsync<ApiResponse<SkuDetailResponse>>(ApiRoutes.Skus.GetById(skuId), ct);

    /// <summary>
    /// Tạo sản phẩm mới
    /// </summary>
    public Task<ApiResponse<Guid>?> CreateProductAsync(CreateProductRequest request, CancellationToken ct = default)
        => PostAsync<ApiResponse<Guid>>(ApiRoutes.Products.Base, request, ct);

    /// <summary>
    /// Cập nhật sản phẩm
    /// </summary>
    public Task<ApiResponse<Guid>?> UpdateProductAsync(Guid id, UpdateProductRequest request, CancellationToken ct = default)
        => PutAsync<ApiResponse<Guid>>(ApiRoutes.Products.GetById(id), request, ct);

    /// <summary>
    /// Tạo SKU mới cho sản phẩm
    /// </summary>
    public Task<ApiResponse<Guid>?> CreateSkuAsync(CreateSkuRequest request, CancellationToken ct = default)
        => PostAsync<ApiResponse<Guid>>(ApiRoutes.Skus.Base, request, ct);

    /// <summary>
    /// Cập nhật SKU
    /// </summary>
    public Task<ApiResponse<Guid>?> UpdateSkuAsync(Guid id, UpdateSkuRequest request, CancellationToken ct = default)
        => PutAsync<ApiResponse<Guid>>(ApiRoutes.Skus.GetById(id), request, ct);

    /// <summary>
    /// Xóa SKU
    /// </summary>
    public Task<ApiResponse<object>?> DeleteSkuAsync(Guid id, CancellationToken ct = default)
        => DeleteAsync<ApiResponse<object>>(ApiRoutes.Skus.GetById(id), ct);
}
