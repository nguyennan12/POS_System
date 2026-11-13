using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using POS.Contracts.V1.Common;
using POS.Contracts.V1.Customers;
using POS.WinUI.Core.Constants;
using POS.WinUI.Core.Services;

namespace POS.WinUI.Core.ApiClients;

/// <summary>
/// Client gọi REST API cho tài nguyên khách hàng & thành viên (Customers & Loyalty)
/// </summary>
public sealed class CustomerApiClient : BaseApiClient
{
    public CustomerApiClient(HttpClient http, SessionService session)
        : base(http, session) { }

    /// <summary>
    /// Tìm kiếm và lọc danh sách khách hàng theo SĐT, tên, mã vạch hoặc hạng thành viên
    /// </summary>
    public Task<ApiResponse<PagedResponse<CustomerSummaryResponse>>?> GetCustomersAsync(
        string? phone = null,
        string? name = null,
        string? barcode = null,
        Guid? memberTierId = null,
        int pageNumber = 1,
        int pageSize = 15,
        CancellationToken ct = default)
    {
        var queryParams = new List<string>
        {
            $"pageNumber={pageNumber}",
            $"pageSize={pageSize}"
        };

        if (!string.IsNullOrWhiteSpace(phone))
            queryParams.Add($"phone={Uri.EscapeDataString(phone.Trim())}");

        if (!string.IsNullOrWhiteSpace(name))
            queryParams.Add($"name={Uri.EscapeDataString(name.Trim())}");

        if (!string.IsNullOrWhiteSpace(barcode))
            queryParams.Add($"barcode={Uri.EscapeDataString(barcode.Trim())}");

        if (memberTierId.HasValue && memberTierId.Value != Guid.Empty)
            queryParams.Add($"memberTierId={memberTierId.Value}");

        var url = $"{ApiRoutes.Customers.Base}?{string.Join("&", queryParams)}";
        return GetAsync<ApiResponse<PagedResponse<CustomerSummaryResponse>>>(url, ct);
    }

    /// <summary>
    /// Lấy chi tiết khách hàng theo ID
    /// </summary>
    public Task<ApiResponse<CustomerDetailResponse>?> GetCustomerByIdAsync(Guid id, CancellationToken ct = default)
        => GetAsync<ApiResponse<CustomerDetailResponse>>(ApiRoutes.Customers.GetById(id), ct);

    /// <summary>
    /// Lấy thông tin điểm tích lũy & hạng thành viên của khách hàng
    /// </summary>
    public Task<ApiResponse<LoyaltyAccountResponse>?> GetLoyaltyAccountAsync(Guid id, CancellationToken ct = default)
        => GetAsync<ApiResponse<LoyaltyAccountResponse>>(ApiRoutes.Customers.Loyalty(id), ct);

    /// <summary>
    /// Tra cứu lịch sử biến động điểm tích lũy của khách hàng
    /// </summary>
    public Task<ApiResponse<PagedResponse<PointTransactionResponse>>?> GetLoyaltyTransactionsAsync(
        Guid id,
        int pageNumber = 1,
        int pageSize = 20,
        DateTimeOffset? from = null,
        DateTimeOffset? to = null,
        string? type = null,
        CancellationToken ct = default)
    {
        var queryParams = new List<string>
        {
            $"pageNumber={pageNumber}",
            $"pageSize={pageSize}"
        };

        if (from.HasValue)
            queryParams.Add($"from={Uri.EscapeDataString(from.Value.ToString("O"))}");

        if (to.HasValue)
            queryParams.Add($"to={Uri.EscapeDataString(to.Value.ToString("O"))}");

        if (!string.IsNullOrWhiteSpace(type))
            queryParams.Add($"type={Uri.EscapeDataString(type.Trim())}");

        var url = $"{ApiRoutes.Customers.Base}/{id}/loyalty/transactions?{string.Join("&", queryParams)}";
        return GetAsync<ApiResponse<PagedResponse<PointTransactionResponse>>>(url, ct);
    }

    /// <summary>
    /// Điều chỉnh điểm thưởng thủ công (Cộng hoặc Trừ điểm)
    /// </summary>
    public Task<ApiResponse<LoyaltyAccountResponse>?> AdjustPointsAsync(
        Guid id,
        AdjustPointsRequest request,
        CancellationToken ct = default)
        => PostAsync<ApiResponse<LoyaltyAccountResponse>>($"{ApiRoutes.Customers.Base}/{id}/loyalty/adjust", request, ct);

    /// <summary>
    /// Lấy danh sách cấu hình tất cả các hạng thành viên từ hệ thống
    /// </summary>
    public Task<ApiResponse<List<MemberTierResponse>>?> GetMemberTiersAsync(CancellationToken ct = default)
        => GetAsync<ApiResponse<List<MemberTierResponse>>>(ApiRoutes.MemberTiers.Base, ct);

    /// <summary>
    /// Cập nhật chính sách của một hạng thành viên (chỉ dành cho Owner)
    /// </summary>
    public Task<ApiResponse<MemberTierResponse>?> UpdateMemberTierAsync(
        Guid id,
        UpdateMemberTierRequest request,
        CancellationToken ct = default)
        => PutAsync<ApiResponse<MemberTierResponse>>(ApiRoutes.MemberTiers.GetById(id), request, ct);

    /// <summary>
    /// Tạo mới hồ sơ khách hàng
    /// </summary>
    public Task<ApiResponse<CustomerDetailResponse>?> CreateCustomerAsync(CreateCustomerRequest request, CancellationToken ct = default)
        => PostAsync<ApiResponse<CustomerDetailResponse>>(ApiRoutes.Customers.Base, request, ct);

    /// <summary>
    /// Cập nhật thông tin khách hàng
    /// </summary>
    public Task<ApiResponse<CustomerDetailResponse>?> UpdateCustomerAsync(Guid id, UpdateCustomerRequest request, CancellationToken ct = default)
        => PutAsync<ApiResponse<CustomerDetailResponse>>(ApiRoutes.Customers.GetById(id), request, ct);

    /// <summary>
    /// Xóa hoặc ngừng hoạt động khách hàng
    /// </summary>
    public Task<ApiResponse<bool>?> DeleteCustomerAsync(Guid id, CancellationToken ct = default)
        => DeleteAsync<ApiResponse<bool>>(ApiRoutes.Customers.GetById(id), ct);
}
