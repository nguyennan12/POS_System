using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using POS.Contracts.V1.Common;
using POS.Contracts.V1.Promotions;
using POS.WinUI.Core.Constants;
using POS.WinUI.Core.Services;

namespace POS.WinUI.Core.ApiClients;

/// <summary>
/// Client gọi REST API cho chương trình khuyến mãi & phát hành voucher
/// </summary>
public sealed class PromotionApiClient : BaseApiClient
{
    public PromotionApiClient(HttpClient http, SessionService session)
        : base(http, session) { }

    /// <summary>
    /// Lọc danh sách chương trình khuyến mãi theo trạng thái, loại KM, chi nhánh và phân trang
    /// </summary>
    public Task<ApiResponse<PagedResponse<PromotionSummaryResponse>>?> GetPromotionsAsync(
        PromotionFilterRequest request,
        CancellationToken ct = default)
    {
        var queryParams = new List<string>
        {
            $"pageNumber={request.PageNumber}",
            $"pageSize={request.PageSize}"
        };

        if (request.StoreId.HasValue && request.StoreId.Value != Guid.Empty)
            queryParams.Add($"storeId={request.StoreId.Value}");

        if (!string.IsNullOrWhiteSpace(request.Status))
            queryParams.Add($"status={Uri.EscapeDataString(request.Status.Trim())}");

        if (!string.IsNullOrWhiteSpace(request.Type))
            queryParams.Add($"type={Uri.EscapeDataString(request.Type.Trim())}");

        if (request.ActiveAt.HasValue)
            queryParams.Add($"activeAt={Uri.EscapeDataString(request.ActiveAt.Value.ToString("O"))}");

        var url = $"{ApiRoutes.Promotions.Base}?{string.Join("&", queryParams)}";
        return GetAsync<ApiResponse<PagedResponse<PromotionSummaryResponse>>>(url, ct);
    }

    /// <summary>
    /// Lấy chi tiết chương trình khuyến mãi theo ID
    /// </summary>
    public Task<ApiResponse<PromotionDetailResponse>?> GetPromotionByIdAsync(Guid id, CancellationToken ct = default)
        => GetAsync<ApiResponse<PromotionDetailResponse>>(ApiRoutes.Promotions.GetById(id), ct);

    /// <summary>
    /// Tạo mới một chương trình khuyến mãi
    /// </summary>
    public Task<ApiResponse<PromotionDetailResponse>?> CreatePromotionAsync(CreatePromotionRequest request, CancellationToken ct = default)
        => PostAsync<ApiResponse<PromotionDetailResponse>>(ApiRoutes.Promotions.Base, request, ct);

    /// <summary>
    /// Cập nhật chương trình khuyến mãi
    /// </summary>
    public Task<ApiResponse<PromotionDetailResponse>?> UpdatePromotionAsync(Guid id, UpdatePromotionRequest request, CancellationToken ct = default)
        => PutAsync<ApiResponse<PromotionDetailResponse>>(ApiRoutes.Promotions.GetById(id), request, ct);

    /// <summary>
    /// Xóa / Hủy chương trình khuyến mãi
    /// </summary>
    public Task<ApiResponse<bool>?> DeletePromotionAsync(Guid id, CancellationToken ct = default)
        => DeleteAsync<ApiResponse<bool>>(ApiRoutes.Promotions.GetById(id), ct);

    /// <summary>
    /// Lấy danh sách các mã voucher thuộc một chương trình khuyến mãi
    /// </summary>
    public Task<ApiResponse<PagedResponse<VoucherResponse>>?> GetVouchersAsync(
        Guid promotionId,
        VoucherFilterRequest request,
        CancellationToken ct = default)
    {
        var queryParams = new List<string>
        {
            $"pageNumber={request.PageNumber}",
            $"pageSize={request.PageSize}"
        };

        if (!string.IsNullOrWhiteSpace(request.Code))
            queryParams.Add($"code={Uri.EscapeDataString(request.Code.Trim())}");

        if (request.IsActive.HasValue)
            queryParams.Add($"isActive={request.IsActive.Value.ToString().ToLowerInvariant()}");

        var url = $"{ApiRoutes.Promotions.Vouchers(promotionId)}?{string.Join("&", queryParams)}";
        return GetAsync<ApiResponse<PagedResponse<VoucherResponse>>>(url, ct);
    }

    /// <summary>
    /// Lấy danh sách tất cả các mã voucher trên hệ thống (hỗ trợ tìm theo code, lọc trạng thái, phân trang)
    /// </summary>
    public Task<ApiResponse<PagedResponse<VoucherResponse>>?> GetAllVouchersAsync(
        VoucherFilterRequest request,
        CancellationToken ct = default)
    {
        var queryParams = new List<string>
        {
            $"pageNumber={request.PageNumber}",
            $"pageSize={request.PageSize}"
        };

        if (!string.IsNullOrWhiteSpace(request.Code))
            queryParams.Add($"code={Uri.EscapeDataString(request.Code.Trim())}");

        if (request.IsActive.HasValue)
            queryParams.Add($"isActive={request.IsActive.Value.ToString().ToLowerInvariant()}");

        var url = $"{ApiRoutes.Vouchers.Base}?{string.Join("&", queryParams)}";
        return GetAsync<ApiResponse<PagedResponse<VoucherResponse>>>(url, ct);
    }

    /// <summary>
    /// Phát hành mã voucher mới thuộc một chương trình khuyến mãi
    /// </summary>
    public Task<ApiResponse<VoucherResponse>?> CreateVoucherAsync(
        Guid promotionId,
        CreateVoucherRequest request,
        CancellationToken ct = default)
        => PostAsync<ApiResponse<VoucherResponse>>(ApiRoutes.Promotions.Vouchers(promotionId), request, ct);
}
