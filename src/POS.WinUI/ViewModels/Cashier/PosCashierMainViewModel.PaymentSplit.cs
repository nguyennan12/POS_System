using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using POS.WinUI.Core.Models;
using Wpf.Ui.Controls;

namespace POS.WinUI.ViewModels.Cashier;

public partial class PosCashierMainViewModel
{
    // ════════════════════ SPLIT PAYMENT (CHIA PHƯƠNG THỨC THANH TOÁN) ════════════════════

    [ObservableProperty]
    private bool _isSplitPayment = false;

    [ObservableProperty]
    private ObservableCollection<PosSplitPaymentItem> _splitPayments = new();

    [ObservableProperty]
    private decimal _totalSplitAllocated = 0;

    [ObservableProperty]
    private decimal _remainingSplitAmount = 0;

    [ObservableProperty]
    private bool _isSplitFullyAllocated = true;

    public bool HasCashInSplit => SplitPayments.Any(p => p.Method == "Cash");
    public bool IsVietQrInSplit => SplitPayments.Any(p => p.Method == "VietQR");
    public bool IsMoMoInSplit => SplitPayments.Any(p => p.Method == "MoMo");
    public bool HasQrInSplit => IsVietQrInSplit || IsMoMoInSplit;

    public bool CanAddCash => IsSplitPayment && !HasCashInSplit;
    public bool CanAddVietQr => IsSplitPayment && !IsVietQrInSplit && !IsMoMoInSplit;
    public bool CanAddMoMo => IsSplitPayment && !IsMoMoInSplit && !IsVietQrInSplit;
    public bool CanAddCard => IsSplitPayment && !SplitPayments.Any(p => p.Method == "Card");

    public bool IsSplitOverAllocated => RemainingSplitAmount < 0;
    public decimal OverAllocatedAmount => Math.Max(0, TotalSplitAllocated - GrandTotal);

    partial void OnIsSplitPaymentChanged(bool value)
    {
        if (value)
        {
            InitializeSplitPayments();
        }
        else
        {
            // Trở về chế độ đơn
            if (SplitPayments.Count > 0)
            {
                var first = SplitPayments[0];
                SelectedPaymentMethod = first.Method;
                UpdatePaymentMethodInfo(first.Method);
            }
            else
            {
                SelectedPaymentMethod = "Cash";
                UpdatePaymentMethodInfo("Cash");
            }

            TenderedCash = GrandTotal;
            TenderedCashInput = GrandTotal.ToString("N0", CultureInfo.InvariantCulture);
            ChangeAmount = 0;
        }
    }

    private void InitializeSplitPayments()
    {
        SplitPayments.Clear();
        var initialItem = CreateSplitItem("Cash", GrandTotal);
        SplitPayments.Add(initialItem);
        RecalculateSplitTotals();
    }

    private PosSplitPaymentItem CreateSplitItem(string method, decimal amount)
    {
        var item = new PosSplitPaymentItem
        {
            Method = method,
            Amount = amount,
            AmountInput = amount.ToString("N0", CultureInfo.InvariantCulture),
            TenderedCash = amount,
            TenderedCashInput = amount.ToString("N0", CultureInfo.InvariantCulture),
            ChangeAmount = 0,
            OnAmountChangedCallback = RecalculateSplitTotals
        };

        switch (method)
        {
            case "VietQR":
                item.Title = "VietQR Banking";
                item.Icon = SymbolRegular.QrCode24;
                item.ImagePath = "/Resources/Assets/ic_vietqr.png";
                item.IconColor = "#0284C7";
                item.IconBackground = "#FFFFFF";
                item.BadgeBg = "#E0F2FE";
                item.BadgeFg = "#0369A1";
                break;
            case "MoMo":
                item.Title = "Ví MoMo";
                item.Icon = SymbolRegular.Wallet24;
                item.ImagePath = "/Resources/Assets/ic_momo.png";
                item.IconColor = "#DB2777";
                item.IconBackground = "#FFFFFF";
                item.BadgeBg = "#FCE7F3";
                item.BadgeFg = "#BE185D";
                break;
            case "Card":
                item.Title = "Thẻ (Card)";
                item.Icon = SymbolRegular.Payment24;
                item.IconColor = "#EA580C";
                item.IconBackground = "#FFFFFF";
                item.BadgeBg = "#FFEDD5";
                item.BadgeFg = "#C2410C";
                break;
            case "Points":
                item.Title = "Điểm tích lũy";
                item.Icon = SymbolRegular.Star24;
                item.IconColor = "#F59E0B";
                item.IconBackground = "#FFFFFF";
                item.BadgeBg = "#FEF3C7";
                item.BadgeFg = "#B45309";
                break;
            case "Cash":
            default:
                item.Title = "Tiền mặt";
                item.Icon = SymbolRegular.Money24;
                item.IconColor = "#16A34A";
                item.IconBackground = "#FFFFFF";
                item.BadgeBg = "#DCFCE7";
                item.BadgeFg = "#15803D";
                break;
        }

        return item;
    }

    [RelayCommand]
    private void AddSplitPaymentMethod(string method)
    {
        if (string.IsNullOrWhiteSpace(method)) return;
        AddOrFocusSplitMethod(method);
    }

    private void AddOrFocusSplitMethod(string method)
    {
        var existing = SplitPayments.FirstOrDefault(p => p.Method.Equals(method, StringComparison.OrdinalIgnoreCase));
        if (existing != null)
        {
            ShowInfo($"Phương thức {existing.Title} đã có trong danh sách phân bổ");
            return;
        }

        if (method == "VietQR" && IsMoMoInSplit)
        {
            ShowWarning("Chỉ được chọn 1 phương thức quét mã QR (VietQR hoặc MoMo) trong cùng một hóa đơn!");
            return;
        }

        if (method == "MoMo" && IsVietQrInSplit)
        {
            ShowWarning("Chỉ được chọn 1 phương thức quét mã QR (VietQR hoặc MoMo) trong cùng một hóa đơn!");
            return;
        }

        // Tự động phân bổ số tiền còn thiếu (Remaining Amount)
        var allocateAmount = Math.Max(0, RemainingSplitAmount);
        var newItem = CreateSplitItem(method, allocateAmount);
        SplitPayments.Add(newItem);

        RecalculateSplitTotals();
    }

    [RelayCommand]
    private void AutoSplitEqually()
    {
        var count = SplitPayments.Count;
        if (count == 0 || GrandTotal <= 0) return;

        var baseShare = Math.Floor(GrandTotal / count);
        var remainder = GrandTotal - (baseShare * count);

        for (int i = 0; i < count; i++)
        {
            var amt = baseShare + (i == 0 ? remainder : 0);
            SplitPayments[i].Amount = amt;
            SplitPayments[i].AmountInput = amt.ToString("N0", CultureInfo.InvariantCulture);
            if (SplitPayments[i].IsCash)
            {
                SplitPayments[i].TenderedCash = amt;
                SplitPayments[i].TenderedCashInput = amt.ToString("N0", CultureInfo.InvariantCulture);
                SplitPayments[i].ChangeAmount = 0;
            }
        }

        RecalculateSplitTotals();
    }

    [RelayCommand]
    private void FillRemainingAmount(PosSplitPaymentItem? item)
    {
        if (item == null) return;
        var target = Math.Max(0, item.Amount + RemainingSplitAmount);
        item.Amount = target;
        item.AmountInput = target.ToString("N0", CultureInfo.InvariantCulture);
        if (item.IsCash)
        {
            item.TenderedCash = target;
            item.TenderedCashInput = target.ToString("N0", CultureInfo.InvariantCulture);
            item.ChangeAmount = 0;
        }
        RecalculateSplitTotals();
    }

    [RelayCommand]
    private void ResetSplitPayments()
    {
        InitializeSplitPayments();
    }

    [RelayCommand]
    private void RemoveSplitPaymentItem(PosSplitPaymentItem? item)
    {
        if (item == null) return;

        if (SplitPayments.Count <= 1)
        {
            ShowWarning("Cần giữ lại ít nhất một phương thức thanh toán!");
            return;
        }

        SplitPayments.Remove(item);
        RecalculateSplitTotals();
    }

    private void RecalculateSplitTotals()
    {
        TotalSplitAllocated = SplitPayments.Sum(p => p.Amount);
        RemainingSplitAmount = GrandTotal - TotalSplitAllocated;
        IsSplitFullyAllocated = (RemainingSplitAmount == 0 && SplitPayments.Count > 0);
        // Cập nhật tỷ lệ % từng dòng
        foreach (var item in SplitPayments)
        {
            if (GrandTotal > 0)
            {
                var pct = (double)(item.Amount / GrandTotal) * 100.0;
                item.Percentage = Math.Max(0, Math.Min(100, pct));
                item.PercentageText = $"{item.Percentage:0.#}%";
            }
            else
            {
                item.Percentage = 0;
                item.PercentageText = "0%";
            }
        }

        // Đồng bộ số tiền thối nếu có phần tiền mặt trong Split Payment
        var cashItem = SplitPayments.FirstOrDefault(p => p.IsCash);
        if (cashItem != null)
        {
            ChangeAmount = Math.Max(0, cashItem.TenderedCash - cashItem.Amount);
        }
        else
        {
            ChangeAmount = 0;
        }

        OnPropertyChanged(nameof(HasCashInSplit));
        OnPropertyChanged(nameof(IsVietQrInSplit));
        OnPropertyChanged(nameof(IsMoMoInSplit));
        OnPropertyChanged(nameof(HasQrInSplit));
        OnPropertyChanged(nameof(CanAddCash));
        OnPropertyChanged(nameof(CanAddVietQr));
        OnPropertyChanged(nameof(CanAddMoMo));
        OnPropertyChanged(nameof(CanAddCard));
        OnPropertyChanged(nameof(IsSplitOverAllocated));
        OnPropertyChanged(nameof(OverAllocatedAmount));

        if (IsPaymentModalOpen)
        {
            _ = GenerateDynamicQrsAsync();
        }
    }
}
