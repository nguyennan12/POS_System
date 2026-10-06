using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using POS.Contracts.V1.Orders;
using POS.WinUI.Core.Models;

namespace POS.WinUI.ViewModels.Cashier;

public partial class PosCashierMainViewModel
{
    // ════════════════════ PAYMENT MODAL & CHECKOUT ORCHESTRATION ════════════════════

    [ObservableProperty]
    private bool _isPaymentModalOpen = false;

    [ObservableProperty]
    private bool _isProcessingCheckout = false;

    [ObservableProperty]
    private bool _shouldPrintReceipt = true;

    /// <summary>
    /// Mở Dialog Thanh toán khi bấm nút Thanh toán hoặc F1
    /// </summary>
    [RelayCommand]
    private void Checkout()
    {
        if (CartItems.Count == 0)
        {
            ShowWarning("Giỏ hàng đang trống! Vui lòng chọn sản phẩm trước khi thanh toán.");
            return;
        }

        if (CurrentShiftId == null)
        {
            ShowError("Chưa mở ca làm việc! Vui lòng kiểm tra lại ca bán hàng.");
            return;
        }

        // Khởi tạo điểm dùng trong checkout
        var basePayable = Math.Max(0, SubTotal - VoucherDiscount);
        var maxPointsForBill = (decimal)Math.Ceiling((double)basePayable / 1000.0);
        MaxRedeemablePoints = Math.Min(CustomerLoyaltyPoints, maxPointsForBill);

        if (HasSelectedCustomer && CustomerLoyaltyPoints > 0 && UseLoyaltyPoints)
        {
            PointsToRedeem = Math.Min(CustomerLoyaltyPoints, (int)Math.Ceiling(LoyaltyDiscount / 1000m));
        }
        else
        {
            PointsToRedeem = 0;
        }
        PointsToRedeemInput = PointsToRedeem.ToString("N0", CultureInfo.InvariantCulture);
        RecalculatePoints();

        // Mặc định chọn tiền mặt và gán sẵn tiền khách đưa = Tổng tiền
        SelectedPaymentMethod = "Cash";
        UpdatePaymentMethodInfo("Cash");
        IsSplitPayment = false;

        TenderedCash = GrandTotal;
        TenderedCashInput = GrandTotal.ToString("N0", CultureInfo.InvariantCulture);
        ChangeAmount = 0;

        // Khởi tạo danh sách Split Payments với phương thức mặc định
        InitializeSplitPayments();

        OnPropertyChanged(nameof(HasCustomerPoints));
        OnPropertyChanged(nameof(HasPointsDiscount));
        OnPropertyChanged(nameof(HasVoucherDiscount));

        IsPaymentModalOpen = true;
    }

    [RelayCommand]
    private void ClosePaymentModal()
    {
        IsPaymentModalOpen = false;
    }

    /// <summary>
    /// Thực hiện gọi API xử lý đơn hàng và hoàn tất thanh toán
    /// </summary>
    [RelayCommand]
    private async Task ConfirmPaymentAsync()
    {
        if (CartItems.Count == 0)
        {
            ShowWarning("Giỏ hàng đang trống! Vui lòng chọn sản phẩm trước khi thanh toán.");
            IsPaymentModalOpen = false;
            return;
        }

        if (CurrentShiftId == null)
        {
            ShowError("Chưa mở ca làm việc! Vui lòng kiểm tra lại ca bán hàng.");
            IsPaymentModalOpen = false;
            return;
        }

        PaymentSplitRequest[] splitRequests;

        if (IsSplitPayment)
        {
            // 1. Kiểm tra tổng phân bổ
            if (RemainingSplitAmount != 0 || SplitPayments.Count == 0)
            {
                ShowWarning($"Tổng tiền phân bổ ({TotalSplitAllocated:N0} đ) chưa khớp với tổng thanh toán ({GrandTotal:N0} đ)!");
                return;
            }

            // 2. Validate dòng tiền mặt nếu có
            var cashItem = SplitPayments.FirstOrDefault(p => p.IsCash);
            if (cashItem != null && cashItem.TenderedCash < cashItem.Amount)
            {
                ShowWarning($"Tiền mặt khách đưa ({cashItem.TenderedCash:N0} đ) chưa đủ phần tiền mặt cần thu ({cashItem.Amount:N0} đ)!");
                return;
            }

            var list = new List<PaymentSplitRequest>();
            if (PointsDiscountAmount > 0)
            {
                list.Add(new PaymentSplitRequest("Points", PointsDiscountAmount));
            }

            list.AddRange(SplitPayments
                .Where(p => p.Amount > 0)
                .Select(p => new PaymentSplitRequest(p.Method, p.Amount)));

            splitRequests = list.ToArray();
        }
        else
        {
            // Validate số tiền khách đưa nếu là tiền mặt
            if (SelectedPaymentMethod == "Cash" && GrandTotal > 0 && TenderedCash < GrandTotal)
            {
                ShowWarning($"Số tiền khách đưa ({TenderedCash:N0} đ) chưa đủ để thanh toán ({GrandTotal:N0} đ)!");
                return;
            }

            var list = new List<PaymentSplitRequest>();
            if (PointsDiscountAmount > 0)
            {
                list.Add(new PaymentSplitRequest("Points", PointsDiscountAmount));
            }

            if (GrandTotal > 0)
            {
                list.Add(new PaymentSplitRequest(SelectedPaymentMethod, GrandTotal));
            }
            else if (list.Count == 0)
            {
                list.Add(new PaymentSplitRequest(SelectedPaymentMethod, 0));
            }

            splitRequests = list.ToArray();
        }

        IsProcessingCheckout = true;

        try
        {
            // 1. Tạo đơn hàng mới
            var note = string.IsNullOrWhiteSpace(OrderNote) ? null : OrderNote.Trim();
            var createRes = await _orderApiClient.CreateOrderAsync(new CreateOrderRequest(CurrentShiftId.Value, SelectedCustomerId, note));
            if (createRes?.Success != true || createRes.Data == null)
            {
                var errMsg = createRes?.Error?.Message ?? "Không thể tạo đơn hàng";
                ShowError(errMsg);
                return;
            }

            var orderId = createRes.Data.Id;

            // 2. Thêm từng sản phẩm vào giỏ hàng trên server
            foreach (var item in CartItems)
            {
                var addRes = await _orderApiClient.AddItemAsync(orderId, new AddOrderItemRequest(item.ProductId, item.Quantity));
                if (addRes?.Success != true)
                {
                    var errMsg = addRes?.Error?.Message ?? "Không thể thêm sản phẩm vào đơn hàng";
                    ShowError(errMsg);
                    return;
                }
            }

            // 3. Áp dụng voucher nếu có
            if (!string.IsNullOrWhiteSpace(VoucherCode) && VoucherDiscount > 0)
            {
                var voucherRes = await _orderApiClient.ApplyVoucherAsync(orderId, new ApplyVoucherRequest(VoucherCode.Trim()));
                if (voucherRes?.Success != true)
                {
                    var errMsg = voucherRes?.Error?.Message ?? "Không thể áp dụng mã giảm giá";
                    ShowError(errMsg);
                    return;
                }
            }

            // 4. Hoàn tất thanh toán với phương thức đã chọn (hoặc split)
            var checkoutReq = new CheckoutOrderRequest(splitRequests, SelectedCustomerId);

            var checkoutRes = await _orderApiClient.CheckoutAsync(orderId, checkoutReq);
            if (checkoutRes?.Success == true && checkoutRes.Data != null)
            {
                IsPaymentModalOpen = false;
                ShowSuccess($"Thanh toán thành công! Tổng tiền: {GrandTotal:N0} đ");
                ClearCart();
                QuickSelectCustomer("Retail");
                _ = FetchProductsFromApiAsync();
            }
            else
            {
                var errMsg = checkoutRes?.Error?.Message ?? "Thanh toán thất bại";
                ShowError(errMsg);
            }
        }
        catch (Exception ex)
        {
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
        ShowInfo("Đang chuẩn bị lệnh in hóa đơn...");
    }

    [RelayCommand]
    private async Task ScanBarcodeAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchQuery))
        {
            ShowInfo("Nhập mã vạch vào ô tìm kiếm rồi nhấn Quét mã vạch");
            return;
        }

        var code = SearchQuery.Trim();

        try
        {
            var res = await _productApiClient.GetSkuByBarcodeAsync(code);
            if (res?.Success == true && res.Data != null)
            {
                var sku = res.Data;
                var productItem = new PosProductItem
                {
                    Id = sku.Id,
                    Name = sku.ProductName,
                    Specification = sku.BaseUnit,
                    Sku = sku.SkuCode,
                    Barcode = sku.Barcode,
                    Unit = sku.BaseUnit,
                    Price = sku.SellPrice,
                    StockQuantity = (int)sku.QtyOnHand,
                    CategoryId = string.Empty,
                    CategoryName = string.Empty
                };

                var prevQty = CartItems.FirstOrDefault(c => c.ProductId == productItem.Id)?.Quantity ?? 0;
                AddToCart(productItem);
                var newQty = CartItems.FirstOrDefault(c => c.ProductId == productItem.Id)?.Quantity ?? 0;

                if (newQty > prevQty)
                {
                    SearchQuery = string.Empty;
                    ShowSuccess($"Đã thêm {sku.ProductName} vào giỏ hàng");
                }
            }
            else
            {
                var errMsg = res?.Error?.Message ?? "Không tìm thấy sản phẩm với mã vạch này";
                ShowWarning(errMsg);
            }
        }
        catch (Exception ex)
        {
            ShowError($"Lỗi quét mã vạch: {ex.Message}");
        }
    }
}
