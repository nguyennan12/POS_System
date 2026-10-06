using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using POS.WinUI.Core.Models;

namespace POS.WinUI.ViewModels.Cashier;

public partial class PosCashierMainViewModel
{
    // ════════════════════ GIỎ HÀNG (CART) ════════════════════

    public ObservableCollection<PosCartItem> CartItems { get; } = new();

    [ObservableProperty]
    private PosCartItem? _selectedCartItem;

    [ObservableProperty]
    private int _cartItemCount = 0;

    [ObservableProperty]
    private bool _isCartEmpty = true;

    [ObservableProperty]
    private decimal _subTotal = 0;

    [ObservableProperty]
    private decimal _totalDiscount = 0;

    [ObservableProperty]
    private decimal _taxTotal = 0;

    [ObservableProperty]
    private decimal _grandTotal = 0;

    [RelayCommand]
    private void AddToCart(PosProductItem? product)
    {
        if (product == null) return;

        if (product.StockQuantity <= 0)
        {
            ShowWarning($"Sản phẩm '{product.Name}' đã hết hàng trong kho.");
            return;
        }

        var existing = CartItems.FirstOrDefault(c => c.ProductId == product.Id);
        if (existing != null)
        {
            if (existing.Quantity >= product.StockQuantity)
            {
                ShowWarning($"Sản phẩm '{product.Name}' chỉ còn {product.StockQuantity} trong kho.");
                return;
            }

            existing.Quantity++;
            SelectedCartItem = existing;
        }
        else
        {
            var newItem = new PosCartItem
            {
                ProductId = product.Id,
                Name = product.Name,
                Sku = product.Sku,
                Unit = product.Unit,
                Price = product.Price,
                StockQuantity = product.StockQuantity,
                Quantity = 1
            };
            CartItems.Add(newItem);
            SelectedCartItem = newItem;
        }

        RecalculateTotals();
    }

    [RelayCommand]
    private void IncreaseQuantity(PosCartItem? item)
    {
        if (item == null) return;

        if (item.StockQuantity > 0 && item.Quantity >= item.StockQuantity)
        {
            ShowWarning($"Sản phẩm '{item.Name}' chỉ còn {item.StockQuantity} trong kho.");
            return;
        }

        item.Quantity++;
        RecalculateTotals();
    }

    [RelayCommand]
    private void DecreaseQuantity(PosCartItem? item)
    {
        if (item == null) return;
        if (item.Quantity > 1)
        {
            item.Quantity--;
        }
        else
        {
            CartItems.Remove(item);
            if (SelectedCartItem == item)
            {
                SelectedCartItem = CartItems.LastOrDefault();
            }
        }
        RecalculateTotals();
    }

    [RelayCommand]
    private void RemoveCartItem(PosCartItem? item)
    {
        if (item == null) return;
        CartItems.Remove(item);
        if (SelectedCartItem == item)
        {
            SelectedCartItem = CartItems.LastOrDefault();
        }
        RecalculateTotals();
    }

    [RelayCommand]
    private void ClearCart()
    {
        CartItems.Clear();
        SelectedCartItem = null;
        VoucherCode = string.Empty;
        VoucherDiscount = 0;
        VoucherMessage = string.Empty;
        OrderNote = string.Empty;
        RecalculateTotals();
    }

    private void RecalculateTotals()
    {
        CartItemCount = CartItems.Sum(c => c.Quantity);
        IsCartEmpty = CartItems.Count == 0;
        SubTotal = CartItems.Sum(c => c.SubTotal);

        // Khách có thể dùng điểm khi có tài khoản, điểm > 0 và giỏ hàng > 0
        CanUseLoyaltyPoints = HasSelectedCustomer && CustomerLoyaltyPoints > 0 && SubTotal > 0;

        if (UseLoyaltyPoints && CanUseLoyaltyPoints)
        {
            // Tiền còn lại cần thanh toán sau voucher
            decimal payableAfterVoucher = Math.Max(0, SubTotal - VoucherDiscount);
            // Quy đổi điểm sang VNĐ (1 điểm = 1.000đ)
            decimal maxPointsValue = CustomerLoyaltyPoints * 1000m;
            // Dùng tối đa không vượt quá số tiền còn lại của đơn hàng
            LoyaltyDiscount = Math.Min(maxPointsValue, payableAfterVoucher);

            LoyaltyUseDisplayText = LoyaltyDiscount > 0 ? $"Dùng (-{LoyaltyDiscount:N0}đ)" : "Dùng điểm";
        }
        else
        {
            LoyaltyDiscount = 0;
            if (!CanUseLoyaltyPoints && UseLoyaltyPoints)
            {
                UseLoyaltyPoints = false;
            }
            LoyaltyUseDisplayText = "Dùng điểm";
        }

        TotalDiscount = VoucherDiscount + LoyaltyDiscount;
        GrandTotal = Math.Max(0, SubTotal - TotalDiscount);

        if (NumpadMode == "Tiền KH")
        {
            UpdateChangeCalculation();
        }
    }
}
