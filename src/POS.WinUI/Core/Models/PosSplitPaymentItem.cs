using System;
using System.Globalization;
using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;
using Wpf.Ui.Controls;

namespace POS.WinUI.Core.Models;

/// <summary>
/// Model cho từng dòng phương thức thanh toán trong chế độ Split Payment
/// </summary>
public partial class PosSplitPaymentItem : ObservableObject
{
    public string Method { get; set; } = "Cash";
    public string Title { get; set; } = "Tiền mặt";
    public SymbolRegular Icon { get; set; } = SymbolRegular.Money24;
    public string IconColor { get; set; } = "#16A34A";
    public string IconBackground { get; set; } = "#FFFFFF";
    public string ImagePath { get; set; } = string.Empty;
    public bool HasImage => !string.IsNullOrWhiteSpace(ImagePath);

    [ObservableProperty]
    private string _badgeBg = "#DCFCE7";

    [ObservableProperty]
    private string _badgeFg = "#166534";

    [ObservableProperty]
    private double _percentage = 0;

    [ObservableProperty]
    private string _percentageText = "0%";

    [ObservableProperty]
    private decimal _amount = 0;

    [ObservableProperty]
    private string _amountInput = "0";

    [ObservableProperty]
    private decimal _tenderedCash = 0;

    [ObservableProperty]
    private string _tenderedCashInput = "0";

    [ObservableProperty]
    private decimal _changeAmount = 0;

    public bool IsCash => Method == "Cash";
    public bool IsVietQr => Method == "VietQR";
    public bool IsMoMo => Method == "MoMo";
    public bool IsQrMethod => IsVietQr || IsMoMo;

    public Action? OnAmountChangedCallback { get; set; }

    partial void OnAmountInputChanged(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            _amount = 0;
            OnPropertyChanged(nameof(Amount));
            OnAmountChangedCallback?.Invoke();
            return;
        }

        var cleanDigits = Regex.Replace(value, @"[^\d]", "");
        if (decimal.TryParse(cleanDigits, out var parsed))
        {
            _amount = parsed;
            OnPropertyChanged(nameof(Amount));
        }
        else
        {
            _amount = 0;
            OnPropertyChanged(nameof(Amount));
        }

        if (IsCash)
        {
            ChangeAmount = Math.Max(0, TenderedCash - Amount);
        }

        OnAmountChangedCallback?.Invoke();
    }

    partial void OnTenderedCashInputChanged(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            _tenderedCash = 0;
            OnPropertyChanged(nameof(TenderedCash));
            ChangeAmount = 0;
            return;
        }

        var cleanDigits = Regex.Replace(value, @"[^\d]", "");
        if (decimal.TryParse(cleanDigits, out var parsed))
        {
            _tenderedCash = parsed;
            OnPropertyChanged(nameof(TenderedCash));
            ChangeAmount = Math.Max(0, TenderedCash - Amount);
        }
        else
        {
            _tenderedCash = 0;
            OnPropertyChanged(nameof(TenderedCash));
            ChangeAmount = 0;
        }
    }
}
