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

///  
/// Client gọi REST API cho tài nguyên khách hàng & thành viên (Customers & Loyalty)
/// </summary>
public sealed class CustomerApiClient : BaseApiClient
{
  public CustomerApiClient(HttpClient http, SessionService session)
      : base(http, session) { }


  /// Tìm kiếm khách hàng theo SĐT hoặc tên
  /// </summary>
  public Task<ApiResponse<PagedResponse<CustomerSummaryResponse>>?> GetCustomersAsync(
      string? phone = null,
      string? name = null,
      int pageNumber = 1,
      int pageSize = 10,
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

    var url = $"{ApiRoutes.Customers.Base}?{string.Join("&", queryParams)}";
    return GetAsync<ApiResponse<PagedResponse<CustomerSummaryResponse>>>(url, ct);
  }


  /// Lấy chi tiết khách hàng theo ID
  /// </summary>
  public Task<ApiResponse<CustomerDetailResponse>?> GetCustomerByIdAsync(Guid id, CancellationToken ct = default)
      => GetAsync<ApiResponse<CustomerDetailResponse>>(ApiRoutes.Customers.GetById(id), ct);


  /// Lấy thông tin điểm tích lũy & hạng thành viên của khách hàng
  /// </summary>
  public Task<ApiResponse<LoyaltyAccountResponse>?> GetLoyaltyAccountAsync(Guid id, CancellationToken ct = default)
      => GetAsync<ApiResponse<LoyaltyAccountResponse>>(ApiRoutes.Customers.Loyalty(id), ct);


  /// Tạo mới hồ sơ khách hàng
  /// </summary>
  public Task<ApiResponse<CustomerDetailResponse>?> CreateCustomerAsync(CreateCustomerRequest request, CancellationToken ct = default)
      => PostAsync<ApiResponse<CustomerDetailResponse>>(ApiRoutes.Customers.Base, request, ct);
}
