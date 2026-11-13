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
/// Client gọi REST API cho Quản lý tồn kho, Nhập hàng, Kiểm kê và Cảnh báo kho (T33)
/// </summary>
public sealed class InventoryApiClient : BaseApiClient
{
    public InventoryApiClient(HttpClient http, SessionService session)
        : base(http, session) { }

    // ── 1. Tồn kho & Cảnh báo (Stock & Alerts) ─────────────────────────

    /// <summary>
    /// Lấy danh sách tồn kho phân trang kèm bộ lọc
    /// </summary>
    public Task<ApiResponse<PagedResponse<StockEntryResponse>>?> GetStockAsync(
        Guid? categoryId = null,
        string? search = null,
        int pageNumber = 1,
        int pageSize = 50,
        bool onlyAlerts = false,
        Guid? storeId = null,
        CancellationToken ct = default)
    {
        var queryParams = new List<string>
        {
            $"pageNumber={pageNumber}",
            $"pageSize={pageSize}"
        };

        if (categoryId.HasValue && categoryId.Value != Guid.Empty)
            queryParams.Add($"categoryId={categoryId.Value}");

        if (storeId.HasValue && storeId.Value != Guid.Empty)
            queryParams.Add($"storeId={storeId.Value}");

        if (!string.IsNullOrWhiteSpace(search))
            queryParams.Add($"search={Uri.EscapeDataString(search.Trim())}");

        if (onlyAlerts)
            queryParams.Add("onlyAlerts=true");

        var url = $"{ApiRoutes.Inventory.Stock}?{string.Join("&", queryParams)}";
        return GetAsync<ApiResponse<PagedResponse<StockEntryResponse>>>(url, ct);
    }

    /// <summary>
    /// Lấy danh sách cảnh báo tồn kho (Dưới định mức & Hàng cận date)
    /// </summary>
    public Task<ApiResponse<PagedResponse<StockAlertResponse>>?> GetStockAlertsAsync(
        int nearExpiryDays = 30,
        int pageNumber = 1,
        int pageSize = 50,
        CancellationToken ct = default)
    {
        var url = $"{ApiRoutes.Inventory.Alerts}?nearExpiryDays={nearExpiryDays}&pageNumber={pageNumber}&pageSize={pageSize}";
        return GetAsync<ApiResponse<PagedResponse<StockAlertResponse>>>(url, ct);
    }

    /// <summary>
    /// Lấy danh sách lô hàng (StockBatch)
    /// </summary>
    public Task<ApiResponse<PagedResponse<StockBatchResponse>>?> GetStockBatchesAsync(
        Guid? skuId = null,
        DateOnly? expiryBefore = null,
        int pageNumber = 1,
        int pageSize = 50,
        CancellationToken ct = default)
    {
        var queryParams = new List<string>
        {
            $"pageNumber={pageNumber}",
            $"pageSize={pageSize}"
        };

        if (skuId.HasValue && skuId.Value != Guid.Empty)
            queryParams.Add($"skuId={skuId.Value}");

        if (expiryBefore.HasValue)
            queryParams.Add($"expiryBefore={expiryBefore.Value:yyyy-MM-dd}");

        var url = $"{ApiRoutes.Inventory.Batches}?{string.Join("&", queryParams)}";
        return GetAsync<ApiResponse<PagedResponse<StockBatchResponse>>>(url, ct);
    }

    /// <summary>
    /// Lấy lịch sử giao dịch biến động kho
    /// </summary>
    public Task<ApiResponse<PagedResponse<StockTransactionResponse>>?> GetStockTransactionsAsync(
        Guid? skuId = null,
        string? type = null,
        int pageNumber = 1,
        int pageSize = 50,
        CancellationToken ct = default)
    {
        var queryParams = new List<string>
        {
            $"pageNumber={pageNumber}",
            $"pageSize={pageSize}"
        };

        if (skuId.HasValue && skuId.Value != Guid.Empty)
            queryParams.Add($"skuId={skuId.Value}");

        if (!string.IsNullOrWhiteSpace(type))
            queryParams.Add($"type={Uri.EscapeDataString(type.Trim())}");

        var url = $"{ApiRoutes.Inventory.Transactions}?{string.Join("&", queryParams)}";
        return GetAsync<ApiResponse<PagedResponse<StockTransactionResponse>>>(url, ct);
    }

    /// <summary>
    /// Xuất hủy hàng hóa khỏi kho
    /// </summary>
    public Task<ApiResponse<StockTransactionResponse>?> DisposeStockAsync(
        DisposeStockRequest request,
        CancellationToken ct = default)
    {
        return PostAsync<ApiResponse<StockTransactionResponse>>(ApiRoutes.Inventory.Dispose, request, ct);
    }

    // ── 2. Nhập kho (Stock-In Vouchers) ───────────────────────────────

    /// <summary>
    /// Lấy danh sách phiếu nhập kho phân trang
    /// </summary>
    public Task<ApiResponse<PagedResponse<StockInVoucherSummaryResponse>>?> GetStockInVouchersAsync(
        Guid? supplierId = null,
        Guid? storeId = null,
        string? status = null,
        DateTimeOffset? from = null,
        DateTimeOffset? to = null,
        int pageNumber = 1,
        int pageSize = 30,
        CancellationToken ct = default)
    {
        var queryParams = new List<string>
        {
            $"pageNumber={pageNumber}",
            $"pageSize={pageSize}"
        };

        if (supplierId.HasValue && supplierId.Value != Guid.Empty)
            queryParams.Add($"supplierId={supplierId.Value}");

        if (storeId.HasValue && storeId.Value != Guid.Empty)
            queryParams.Add($"storeId={storeId.Value}");

        if (!string.IsNullOrWhiteSpace(status) && status != "Tất cả")
            queryParams.Add($"status={Uri.EscapeDataString(status.Trim())}");

        if (from.HasValue)
            queryParams.Add($"from={Uri.EscapeDataString(from.Value.ToString("o"))}");

        if (to.HasValue)
            queryParams.Add($"to={Uri.EscapeDataString(to.Value.ToString("o"))}");

        var url = $"{ApiRoutes.Inventory.StockIn}?{string.Join("&", queryParams)}";
        return GetAsync<ApiResponse<PagedResponse<StockInVoucherSummaryResponse>>>(url, ct);
    }

    /// <summary>
    /// Lấy chi tiết phiếu nhập kho theo ID
    /// </summary>
    public Task<ApiResponse<StockInVoucherDetailResponse>?> GetStockInVoucherByIdAsync(
        Guid id,
        CancellationToken ct = default)
    {
        return GetAsync<ApiResponse<StockInVoucherDetailResponse>>(ApiRoutes.Inventory.StockInById(id), ct);
    }

    /// <summary>
    /// Tạo phiếu nhập kho mới (Draft)
    /// </summary>
    public Task<ApiResponse<StockInVoucherDetailResponse>?> CreateStockInVoucherAsync(
        CreateStockInVoucherRequest request,
        CancellationToken ct = default)
    {
        return PostAsync<ApiResponse<StockInVoucherDetailResponse>>(ApiRoutes.Inventory.StockIn, request, ct);
    }

    /// <summary>
    /// Hoàn thành phiếu nhập kho: tăng tồn kho, ghi nhật ký giao dịch và cập nhật giá vốn
    /// </summary>
    public Task<ApiResponse<StockInVoucherDetailResponse>?> CompleteStockInVoucherAsync(
        Guid id,
        CancellationToken ct = default)
    {
        return PostAsync<ApiResponse<StockInVoucherDetailResponse>>(ApiRoutes.Inventory.StockInComplete(id), new { }, ct);
    }

    /// <summary>
    /// Hủy phiếu nhập kho (chuyển sang Cancelled)
    /// </summary>
    public Task<ApiResponse<StockInVoucherDetailResponse>?> CancelStockInVoucherAsync(
        Guid id,
        CancellationToken ct = default)
    {
        return PostAsync<ApiResponse<StockInVoucherDetailResponse>>(ApiRoutes.Inventory.StockInCancel(id), new { }, ct);
    }

    /// <summary>
    /// Xem trước bảng kê phiếu nhập kho từ file Excel trước khi chính thức tạo phiếu
    /// </summary>
    public async Task<ApiResponse<StockInImportPreviewResponse>?> PreviewBulkImportStockInAsync(
        string filePath,
        Guid supplierId,
        CancellationToken ct = default)
    {
        AttachToken();
        using var form = new System.Net.Http.MultipartFormDataContent();
        await using var fileStream = System.IO.File.OpenRead(filePath);
        using var streamContent = new System.Net.Http.StreamContent(fileStream);

        var ext = System.IO.Path.GetExtension(filePath).ToLowerInvariant();
        var mimeType = ext == ".xls"
            ? "application/vnd.ms-excel"
            : "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

        streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(mimeType);
        form.Add(streamContent, "file", System.IO.Path.GetFileName(filePath));
        form.Add(new System.Net.Http.StringContent(supplierId.ToString()), "supplierId");

        var response = await _http.PostAsync(ApiRoutes.Inventory.StockInBulkImportPreview, form, ct);
        await HandleFailureResponseAsync(response, ct);
        return await System.Net.Http.Json.HttpContentJsonExtensions.ReadFromJsonAsync<ApiResponse<StockInImportPreviewResponse>>(response.Content, _jsonOptions, ct);
    }

    /// <summary>
    /// Nhập kho hàng loạt từ file Excel bảng kê giao hàng của Nhà Cung Cấp
    /// </summary>
    public async Task<ApiResponse<ImportStockInVoucherResponse>?> BulkImportStockInAsync(
        string filePath,
        Guid supplierId,
        string? note = null,
        bool autoComplete = true,
        CancellationToken ct = default)
    {
        AttachToken();
        using var form = new System.Net.Http.MultipartFormDataContent();
        await using var fileStream = System.IO.File.OpenRead(filePath);
        using var streamContent = new System.Net.Http.StreamContent(fileStream);

        var ext = System.IO.Path.GetExtension(filePath).ToLowerInvariant();
        var mimeType = ext == ".xls"
            ? "application/vnd.ms-excel"
            : "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

        streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(mimeType);
        form.Add(streamContent, "file", System.IO.Path.GetFileName(filePath));
        form.Add(new System.Net.Http.StringContent(supplierId.ToString()), "supplierId");
        if (!string.IsNullOrWhiteSpace(note))
            form.Add(new System.Net.Http.StringContent(note), "note");
        form.Add(new System.Net.Http.StringContent(autoComplete.ToString().ToLowerInvariant()), "autoComplete");

        var response = await _http.PostAsync(ApiRoutes.Inventory.StockInBulkImport, form, ct);
        await HandleFailureResponseAsync(response, ct);
        return await System.Net.Http.Json.HttpContentJsonExtensions.ReadFromJsonAsync<ApiResponse<ImportStockInVoucherResponse>>(response.Content, _jsonOptions, ct);
    }

    // ── 3. Kiểm kê kho (Stock Takes) ─────────────────────────────────

    /// <summary>
    /// Lấy danh sách các đợt kiểm kê kho phân trang
    /// </summary>
    public Task<ApiResponse<PagedResponse<StockTakeSummaryResponse>>?> GetStockTakesAsync(
        Guid? storeId = null,
        string? status = null,
        DateTimeOffset? from = null,
        DateTimeOffset? to = null,
        int pageNumber = 1,
        int pageSize = 30,
        CancellationToken ct = default)
    {
        var queryParams = new List<string>
        {
            $"pageNumber={pageNumber}",
            $"pageSize={pageSize}"
        };

        if (storeId.HasValue && storeId.Value != Guid.Empty)
            queryParams.Add($"storeId={storeId.Value}");

        if (!string.IsNullOrWhiteSpace(status) && status != "Tất cả")
            queryParams.Add($"status={Uri.EscapeDataString(status.Trim())}");

        if (from.HasValue)
            queryParams.Add($"from={Uri.EscapeDataString(from.Value.ToString("o"))}");

        if (to.HasValue)
            queryParams.Add($"to={Uri.EscapeDataString(to.Value.ToString("o"))}");

        var url = $"{ApiRoutes.Inventory.StockTakes}?{string.Join("&", queryParams)}";
        return GetAsync<ApiResponse<PagedResponse<StockTakeSummaryResponse>>>(url, ct);
    }

    /// <summary>
    /// Lấy chi tiết phiếu kiểm kê kho theo ID kèm danh sách sản phẩm đếm
    /// </summary>
    public Task<ApiResponse<StockTakeDetailResponse>?> GetStockTakeByIdAsync(
        Guid id,
        CancellationToken ct = default)
    {
        return GetAsync<ApiResponse<StockTakeDetailResponse>>(ApiRoutes.Inventory.StockTakeById(id), ct);
    }

    /// <summary>
    /// Tạo đợt kiểm kê kho mới (Draft)
    /// </summary>
    public Task<ApiResponse<StockTakeDetailResponse>?> CreateStockTakeAsync(
        CreateStockTakeRequest request,
        CancellationToken ct = default)
    {
        return PostAsync<ApiResponse<StockTakeDetailResponse>>(ApiRoutes.Inventory.StockTakes, request, ct);
    }

    /// <summary>
    /// Cập nhật số lượng đếm thực tế của các mặt hàng trong phiếu kiểm kê
    /// </summary>
    public Task<ApiResponse<StockTakeDetailResponse>?> UpdateStockTakeItemsAsync(
        Guid id,
        UpdateStockTakeItemsRequest request,
        CancellationToken ct = default)
    {
        return PutAsync<ApiResponse<StockTakeDetailResponse>>(ApiRoutes.Inventory.StockTakeItems(id), request, ct);
    }

    /// <summary>
    /// Gửi phiếu kiểm kê chờ quản lý phê duyệt (Draft -> Pending)
    /// </summary>
    public Task<ApiResponse<StockTakeDetailResponse>?> SubmitStockTakeAsync(
        Guid id,
        CancellationToken ct = default)
    {
        return PostAsync<ApiResponse<StockTakeDetailResponse>>(ApiRoutes.Inventory.StockTakeSubmit(id), new { }, ct);
    }

    /// <summary>
    /// Quản lý phê duyệt chênh lệch kiểm kê và tự động cân bằng tồn kho (Adjust)
    /// </summary>
    public Task<ApiResponse<StockTakeDetailResponse>?> ApproveStockTakeAsync(
        Guid id,
        CancellationToken ct = default)
    {
        return PostAsync<ApiResponse<StockTakeDetailResponse>>(ApiRoutes.Inventory.StockTakeApprove(id), new { }, ct);
    }

    // ── 4. Nhà cung cấp (Suppliers) ──────────────────────────────────

    /// <summary>
    /// Lấy danh sách nhà cung cấp để chọn khi nhập kho
    /// </summary>
    public Task<ApiResponse<PagedResponse<SupplierResponse>>?> GetSuppliersAsync(
        string? search = null,
        int pageNumber = 1,
        int pageSize = 100,
        CancellationToken ct = default)
    {
        var queryParams = new List<string>
        {
            $"pageNumber={pageNumber}",
            $"pageSize={pageSize}",
            "isActive=true"
        };

        if (!string.IsNullOrWhiteSpace(search))
            queryParams.Add($"search={Uri.EscapeDataString(search.Trim())}");

        var url = $"{ApiRoutes.Suppliers.Base}?{string.Join("&", queryParams)}";
        return GetAsync<ApiResponse<PagedResponse<SupplierResponse>>>(url, ct);
    }
}
