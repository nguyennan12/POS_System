using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using POS.Contracts.V1.Common;
using POS.Contracts.V1.Orders;
using POS.WinUI.Core.Constants;
using POS.WinUI.Core.Services;

namespace POS.WinUI.Core.ApiClients;

/// <summary>
/// Client gọi REST API cho tài nguyên đơn hàng & thanh toán (Orders & Checkout)
/// </summary>
public sealed class OrderApiClient : BaseApiClient
{
    public OrderApiClient(HttpClient http, SessionService session)
        : base(http, session) { }

    /// <summary>
    /// Tạo đơn hàng mới ở trạng thái Draft gắn với ca làm việc
    /// </summary>
    public Task<ApiResponse<OrderDetailResponse>?> CreateOrderAsync(CreateOrderRequest request, CancellationToken ct = default)
        => PostAsync<ApiResponse<OrderDetailResponse>>(ApiRoutes.Orders.Base, request, ct);

    /// <summary>
    /// Thêm hoặc cập nhật số lượng SKU vào giỏ hàng
    /// </summary>
    public Task<ApiResponse<OrderDetailResponse>?> AddItemAsync(Guid orderId, AddOrderItemRequest request, CancellationToken ct = default)
        => PostAsync<ApiResponse<OrderDetailResponse>>(ApiRoutes.Orders.Items(orderId), request, ct);

    /// <summary>
    /// Áp dụng mã voucher vào đơn hàng
    /// </summary>
    public Task<ApiResponse<OrderDetailResponse>?> ApplyVoucherAsync(Guid orderId, ApplyVoucherRequest request, CancellationToken ct = default)
        => PostAsync<ApiResponse<OrderDetailResponse>>(ApiRoutes.Orders.Vouchers(orderId), request, ct);

    /// <summary>
    /// Thực hiện thanh toán và hoàn tất đơn hàng
    /// </summary>
    public Task<ApiResponse<CheckoutResponse>?> CheckoutAsync(Guid orderId, CheckoutOrderRequest request, CancellationToken ct = default)
        => PostAsync<ApiResponse<CheckoutResponse>>(ApiRoutes.Orders.Checkout(orderId), request, ct);

    /// <summary>
    /// Hủy đơn hàng
    /// </summary>
    public Task<ApiResponse<OrderDetailResponse>?> CancelOrderAsync(Guid orderId, CancelOrderRequest request, CancellationToken ct = default)
        => PostAsync<ApiResponse<OrderDetailResponse>>(ApiRoutes.Orders.Cancel(orderId), request, ct);

    /// <summary>
    /// Lấy chi tiết đơn hàng theo ID
    /// </summary>
    public Task<ApiResponse<OrderDetailResponse>?> GetOrderByIdAsync(Guid orderId, CancellationToken ct = default)
        => GetAsync<ApiResponse<OrderDetailResponse>>(ApiRoutes.Orders.GetById(orderId), ct);
}
