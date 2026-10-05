using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using POS.Contracts.V1.Orders;

namespace POS.WinUI.ViewModels.Cashier;

public partial class PosCashierMainViewModel
{
    // ════════════════════ THANH TOÁN (CHECKOUT API) ════════════════════

    [ObservableProperty]
    private bool _isProcessingCheckout = false;

    [RelayCommand]
    private async Task CheckoutAsync()
    {
        if (CartItems.Count == 0)
        {
            StatusText = "Giỏ hàng đang trống";
            ShowWarning("Giỏ hàng đang trống! Vui lòng chọn sản phẩm trước khi thanh toán.");
            return;
        }

        if (CurrentShiftId == null)
        {
            StatusText = "Chưa mở ca làm việc. Vui lòng kiểm tra lại ca.";
            ShowError("Chưa mở ca làm việc! Vui lòng kiểm tra lại ca bán hàng.");
            return;
        }

        IsProcessingCheckout = true;
        StatusText = "Đang xử lý đơn hàng qua API...";

        try
        {
            // 1. Tạo đơn hàng mới
            var createRes = await _orderApiClient.CreateOrderAsync(new CreateOrderRequest(CurrentShiftId.Value, SelectedCustomerId));
            if (createRes?.Success != true || createRes.Data == null)
            {
                var errMsg = createRes?.Error?.Message ?? "Không thể tạo đơn hàng";
                StatusText = errMsg;
                ShowError(errMsg);
                return;
            }

            var orderId = createRes.Data.Id;

            // 2. Thêm từng sản phẩm vào giỏ hàng trên server
            foreach (var item in CartItems)
            {
                await _orderApiClient.AddItemAsync(orderId, new AddOrderItemRequest(item.ProductId, item.Quantity));
            }

            // 3. Áp dụng voucher nếu có
            if (!string.IsNullOrWhiteSpace(VoucherCode) && VoucherDiscount > 0)
            {
                await _orderApiClient.ApplyVoucherAsync(orderId, new ApplyVoucherRequest(VoucherCode.Trim()));
            }

            // 4. Hoàn tất thanh toán
            var checkoutReq = new CheckoutOrderRequest(
                new[] { new PaymentSplitRequest("Cash", GrandTotal) },
                SelectedCustomerId);

            var checkoutRes = await _orderApiClient.CheckoutAsync(orderId, checkoutReq);
            if (checkoutRes?.Success == true && checkoutRes.Data != null)
            {
                StatusText = $"Thanh toán thành công! Đơn hàng: {orderId:N}";
                ShowSuccess($"Thanh toán thành công! Tổng tiền: {GrandTotal:N0} đ");
                ClearCart();
                QuickSelectCustomer("Retail");
                _ = FetchProductsFromApiAsync();
            }
            else
            {
                var errMsg = checkoutRes?.Error?.Message ?? "Thanh toán thất bại";
                StatusText = errMsg;
                ShowError(errMsg);
            }
        }
        catch (Exception ex)
        {
            StatusText = $"Lỗi thanh toán: {ex.Message}";
            ShowError($"Lỗi thanh toán: {ex.Message}");
        }
        finally
        {
            IsProcessingCheckout = false;
        }
    }

    [RelayCommand]
    private void PrintBill()
    {
        StatusText = "Đang chuẩn bị lệnh in hóa đơn...";
        ShowInfo("Đang chuẩn bị lệnh in hóa đơn...");
    }

    [RelayCommand]
    private void ScanBarcode()
    {
        StatusText = "Sẵn sàng quét mã vạch...";
        ShowInfo("Sẵn sàng quét mã vạch sản phẩm");
    }
}
