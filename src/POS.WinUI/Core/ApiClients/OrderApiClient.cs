using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using POS.Contracts.V1.Common;
using POS.Contracts.V1.Orders;
using POS.WinUI.Core.Constants;
using POS.WinUI.Core.Services;

namespace POS.WinUI.Core.ApiClients;

///  
/// Client gọi REST API cho tài nguyên đơn hàng & thanh toán (Orders & Checkout)
/// </summary>
public sealed class OrderApiClient : BaseApiClient
{
  public OrderApiClient(HttpClient http, SessionService session)
      : base(http, session) { }


  /// Tạo đơn hàng mới ở trạng thái Draft gắn với ca làm việc
  /// </summary>
  public Task<ApiResponse<OrderDetailResponse>?> CreateOrderAsync(CreateOrderRequest request, CancellationToken ct = default)
      => PostAsync<ApiResponse<OrderDetailResponse>>(ApiRoutes.Orders.Base, request, ct);


  /// Thêm hoặc cập nhật số lượng SKU vào giỏ hàng
  /// </summary>
  public Task<ApiResponse<OrderDetailResponse>?> AddItemAsync(Guid orderId, AddOrderItemRequest request, CancellationToken ct = default)
      => PostAsync<ApiResponse<OrderDetailResponse>>(ApiRoutes.Orders.Items(orderId), request, ct);


  /// Áp dụng mã voucher vào đơn hàng
  /// </summary>
  public Task<ApiResponse<OrderDetailResponse>?> ApplyVoucherAsync(Guid orderId, ApplyVoucherRequest request, CancellationToken ct = default)
      => PostAsync<ApiResponse<OrderDetailResponse>>(ApiRoutes.Orders.Vouchers(orderId), request, ct);


  /// Thực hiện thanh toán và hoàn tất đơn hàng
  /// </summary>
  public Task<ApiResponse<CheckoutResponse>?> CheckoutAsync(Guid orderId, CheckoutOrderRequest request, CancellationToken ct = default)
      => PostAsync<ApiResponse<CheckoutResponse>>(ApiRoutes.Orders.Checkout(orderId), request, ct);


  /// Hủy đơn hàng
  /// </summary>
  public Task<ApiResponse<OrderDetailResponse>?> CancelOrderAsync(Guid orderId, CancelOrderRequest request, CancellationToken ct = default)
      => PostAsync<ApiResponse<OrderDetailResponse>>(ApiRoutes.Orders.Cancel(orderId), request, ct);


  /// Lấy chi tiết đơn hàng theo ID
  /// </summary>
  public Task<ApiResponse<OrderDetailResponse>?> GetOrderByIdAsync(Guid orderId, CancellationToken ct = default)
      => GetAsync<ApiResponse<OrderDetailResponse>>(ApiRoutes.Orders.GetById(orderId), ct);

  /// <summary>
  /// Lấy danh sách đơn hàng có phân trang & bộ lọc (storeId, shiftId, status, from, to)
  /// </summary>
  public Task<ApiResponse<PagedResponse<OrderSummaryResponse>>?> GetOrdersAsync(
      OrderFilterRequest filter, CancellationToken ct = default)
  {
    var queryParams = new System.Collections.Generic.List<string>();
    if (filter.StoreId.HasValue) queryParams.Add($"storeId={filter.StoreId.Value}");
    if (filter.ShiftId.HasValue) queryParams.Add($"shiftId={filter.ShiftId.Value}");
    if (!string.IsNullOrWhiteSpace(filter.Status)) queryParams.Add($"status={Uri.EscapeDataString(filter.Status)}");
    if (filter.From.HasValue) queryParams.Add($"from={Uri.EscapeDataString(filter.From.Value.ToString("O"))}");
    if (filter.To.HasValue) queryParams.Add($"to={Uri.EscapeDataString(filter.To.Value.ToString("O"))}");
    queryParams.Add($"pageNumber={filter.PageNumber}");
    queryParams.Add($"pageSize={filter.PageSize}");

    var url = $"{ApiRoutes.Orders.Base}?{string.Join("&", queryParams)}";
    return GetAsync<ApiResponse<PagedResponse<OrderSummaryResponse>>>(url, ct);
  }
}
