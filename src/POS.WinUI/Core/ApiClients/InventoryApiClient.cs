using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using POS.Contracts.V1.Common;
using POS.Contracts.V1.Inventory;
using POS.WinUI.Core.Constants;
using POS.WinUI.Core.Services;

namespace POS.WinUI.Core.ApiClients;

/// <summary>
/// Client gọi REST API cho tài nguyên tồn kho và danh mục hàng hóa (Inventory & Stock)
/// </summary>
public sealed class InventoryApiClient : BaseApiClient
{
    public InventoryApiClient(HttpClient http, SessionService session)
        : base(http, session) { }

    /// <summary>
    /// Lấy danh sách sản phẩm / tồn kho kèm bộ lọc phân loại và tìm kiếm
    /// </summary>
    public Task<ApiResponse<PagedResponse<StockEntryResponse>>?> GetStockAsync(
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

        var url = $"{ApiRoutes.Inventory.Stock}?{string.Join("&", queryParams)}";
        return GetAsync<ApiResponse<PagedResponse<StockEntryResponse>>>(url, ct);
    }
}
