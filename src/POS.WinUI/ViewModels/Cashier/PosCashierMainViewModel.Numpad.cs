using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace POS.WinUI.ViewModels.Cashier;

public partial class PosCashierMainViewModel
{
    // ════════════════════ BÀN PHÍM SỐ CẢM ỨNG (NUMPAD) ════════════════════

    [ObservableProperty]
    private string _numpadMode = "Tiền KH"; // "Số lượng", "CK %", "Giảm đ", "Tiền KH"

    [ObservableProperty]
    private string _numpadDisplayLabel = "Tiền khách đưa (đ)";

    [ObservableProperty]
    private string _numpadDisplaySubLabel = "Cho đơn hiện tại";

    [ObservableProperty]
    private string _numpadDisplayValue = "0";

    [ObservableProperty]
    private decimal _customerCashGiven = 0;

    [ObservableProperty]
    private decimal _changeAmount = 0;

    [RelayCommand]
    private void SetNumpadMode(string mode)
    {
        NumpadMode = mode;
        NumpadDisplayValue = "0";

        switch (mode)
        {
            case "Tiền KH":
                NumpadDisplayLabel = "Tiền khách đưa (đ)";
                NumpadDisplaySubLabel = "Cho đơn hiện tại";
                break;
            case "Số lượng":
                NumpadDisplayLabel = "Số lượng món";
                NumpadDisplaySubLabel = SelectedCartItem != null ? SelectedCartItem.Name : "Chọn món để sửa SL";
                break;
            case "CK %":
                NumpadDisplayLabel = "Chiết khấu (%)";
                NumpadDisplaySubLabel = "Giảm phần trăm trên đơn";
                break;
            case "Giảm đ":
                NumpadDisplayLabel = "Giảm trực tiếp (đ)";
                NumpadDisplaySubLabel = "Trừ tiền mặt trực tiếp";
                break;
        }
    }

    [RelayCommand]
    private void NumpadInput(string key)
    {
        if (NumpadDisplayValue == "0")
        {
            NumpadDisplayValue = key;
        }
        else
        {
            if (NumpadDisplayValue.Length < 12)
            {
                NumpadDisplayValue += key;
            }
        }

        ApplyNumpadValue();
    }

    [RelayCommand]
    private void NumpadClear()
    {
        NumpadDisplayValue = "0";
        ApplyNumpadValue();
    }

    [RelayCommand]
    private void NumpadBackspace()
    {
        if (NumpadDisplayValue.Length > 1)
        {
            NumpadDisplayValue = NumpadDisplayValue[..^1];
        }
        else
        {
            NumpadDisplayValue = "0";
        }

        ApplyNumpadValue();
    }

    [RelayCommand]
    private void QuickCash(string amountStr)
    {
        NumpadMode = "Tiền KH";
        NumpadDisplayLabel = "Tiền khách đưa (đ)";
        NumpadDisplaySubLabel = "Cho đơn hiện tại";

        if (amountStr == "EXACT")
        {
            CustomerCashGiven = GrandTotal;
            NumpadDisplayValue = GrandTotal.ToString("N0").Replace(",", ".");
        }
        else if (decimal.TryParse(amountStr, out var val))
        {
            CustomerCashGiven = val;
            NumpadDisplayValue = val.ToString("N0").Replace(",", ".");
        }

        UpdateChangeCalculation();
    }

    private void ApplyNumpadValue()
    {
        var rawNumber = NumpadDisplayValue.Replace(".", "").Replace(",", "");
        if (!decimal.TryParse(rawNumber, out var val)) val = 0;

        switch (NumpadMode)
        {
            case "Tiền KH":
                CustomerCashGiven = val;
                UpdateChangeCalculation();
                break;
            case "Số lượng":
                if (SelectedCartItem != null && val > 0)
                {
                    var targetQty = (int)val;
                    if (SelectedCartItem.StockQuantity > 0 && targetQty > SelectedCartItem.StockQuantity)
                    {
                        SelectedCartItem.Quantity = SelectedCartItem.StockQuantity;
                        ShowWarning($"Sản phẩm '{SelectedCartItem.Name}' chỉ còn {SelectedCartItem.StockQuantity} trong kho.");
                    }
                    else
                    {
                        SelectedCartItem.Quantity = targetQty;
                    }
                    RecalculateTotals();
                }
                else if (SelectedCartItem == null)
                {
                    ShowWarning("Vui lòng chọn món trong giỏ hàng để đổi số lượng");
                }
                break;
            case "CK %":
                if (val >= 0 && val <= 100)
                {
                    VoucherDiscount = SubTotal * (val / 100m);
                    RecalculateTotals();
                }
                else if (val > 100)
                {
                    ShowWarning("Chiết khấu không được vượt quá 100%!");
                }
                break;
            case "Giảm đ":
                VoucherDiscount = Math.Min(val, SubTotal);
                RecalculateTotals();
                break;
        }
    }

    private void UpdateChangeCalculation()
    {
        if (CustomerCashGiven >= GrandTotal)
        {
            ChangeAmount = CustomerCashGiven - GrandTotal;
        }
        else
        {
            ChangeAmount = 0;
        }
    }
}
