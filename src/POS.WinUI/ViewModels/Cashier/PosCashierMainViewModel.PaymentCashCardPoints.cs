using System;
using System.Globalization;
using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace POS.WinUI.ViewModels.Cashier;

public partial class PosCashierMainViewModel
{
    // ════════════════════ PAYMENT METHODS: CASH, CARD & POINTS ════════════════════

    [ObservableProperty]
    private string _selectedPaymentMethod = "Cash";

    [ObservableProperty]
    private string _selectedPaymentMethodTitle = "Tiền mặt";

    [ObservableProperty]
    private string _selectedPaymentMethodDescription = "Nhận tiền mặt trực tiếp và trả tiền thừa cho khách";

    [ObservableProperty]
    private decimal _tenderedCash = 0;

    [ObservableProperty]
    private string _tenderedCashInput = "0";

    // ── Card Properties ──
    [ObservableProperty]
    private string _selectedCardType = "Mastercard";

    [ObservableProperty]
    private string _cardTraceNo = string.Empty;

    [ObservableProperty]
    private string _cardStatus = "Sẵn sàng quẹt thẻ / cắm chip";

    // ── Points Properties ──
    [ObservableProperty]
    private decimal _pointsToRedeem = 0;

    [ObservableProperty]
    private string _pointsToRedeemInput = "0";

    [ObservableProperty]
    private decimal _pointsDiscountAmount = 0;

    [ObservableProperty]
    private decimal _pointsRemainingAfter = 0;

    [ObservableProperty]
    private decimal _maxRedeemablePoints = 0;

    public bool HasCustomerPoints => HasSelectedCustomer && CustomerLoyaltyPoints > 0;
    public bool HasPointsDiscount => PointsDiscountAmount > 0;
    public bool HasVoucherDiscount => VoucherDiscount > 0;

    [RelayCommand]
    private void SelectPaymentMethod(string method)
    {
        if (string.IsNullOrWhiteSpace(method)) return;

        if (IsSplitPayment)
        {
            AddOrFocusSplitMethod(method);
        }
        else
        {
            SelectedPaymentMethod = method;
            UpdatePaymentMethodInfo(method);

            if (method == "Cash")
            {
                TenderedCash = GrandTotal;
                TenderedCashInput = GrandTotal.ToString("N0", CultureInfo.InvariantCulture);
                ChangeAmount = 0;
            }
        }
        SyncPaymentToCfd();
    }

    public void UpdatePaymentMethodInfo(string method)
    {
        switch (method)
        {
            case "VietQR":
                SelectedPaymentMethodTitle = "VietQR Banking";
                SelectedPaymentMethodDescription = "Quét mã QR chuyển khoản tự động qua ngân hàng";
                break;
            case "MoMo":
                SelectedPaymentMethodTitle = "Ví MoMo";
                SelectedPaymentMethodDescription = "Mở app MoMo để quét mã thanh toán";
                break;
            case "Card":
                SelectedPaymentMethodTitle = "Thẻ";
                SelectedPaymentMethodDescription = "Mastercard / Visa / NAPAS • Quẹt trên máy POS";
                break;
            case "Cash":
            default:
                SelectedPaymentMethodTitle = "Tiền mặt";
                SelectedPaymentMethodDescription = "Nhận tiền mặt trực tiếp và trả tiền thừa cho khách";
                break;
        }

        InitializeCenterViewForMethod(method);
    }

    private void InitializeCenterViewForMethod(string method)
    {
        switch (method)
        {
            case "VietQR":
                VietQrStatus = "Đang chờ khách quét mã...";
                GenerateDynamicQrs();
                break;
            case "MoMo":
                MoMoStatus = "Đang chờ quét mã ví MoMo...";
                GenerateDynamicQrs();
                break;
            case "Card":
                CardStatus = "Sẵn sàng quẹt thẻ / cắm chip";
                break;
        }
    }

    // ── Card Commands ──
    [RelayCommand]
    private void SelectCardType(string cardType)
    {
        if (string.IsNullOrWhiteSpace(cardType)) return;
        SelectedCardType = cardType;
    }

    // ── Loyalty Points Commands & Calculations (1 điểm = 1.000 VNĐ) ──
    [RelayCommand]
    private void ApplyAllPoints()
    {
        var basePayable = Math.Max(0, SubTotal - VoucherDiscount);
        var maxPointsForBill = (decimal)Math.Ceiling((double)basePayable / 1000.0);
        MaxRedeemablePoints = Math.Min(CustomerLoyaltyPoints, maxPointsForBill);
        PointsToRedeem = MaxRedeemablePoints;
        PointsToRedeemInput = PointsToRedeem.ToString("N0", CultureInfo.InvariantCulture);
        RecalculatePoints();
    }

    [RelayCommand]
    private void ApplyHalfPoints()
    {
        var basePayable = Math.Max(0, SubTotal - VoucherDiscount);
        var maxPointsForBill = (decimal)Math.Ceiling((double)basePayable / 1000.0);
        MaxRedeemablePoints = Math.Min(CustomerLoyaltyPoints, maxPointsForBill);
        PointsToRedeem = Math.Floor(MaxRedeemablePoints / 2);
        PointsToRedeemInput = PointsToRedeem.ToString("N0", CultureInfo.InvariantCulture);
        RecalculatePoints();
    }

    [RelayCommand]
    private void ApplyPointsPreset(object? param)
    {
        if (param == null) return;

        decimal points = 0;
        if (param is decimal d) points = d;
        else if (param is int i) points = i;
        else if (param is long l) points = l;
        else if (decimal.TryParse(param.ToString(), out var pDec)) points = pDec;

        var basePayable = Math.Max(0, SubTotal - VoucherDiscount);
        var maxPointsForBill = (decimal)Math.Ceiling((double)basePayable / 1000.0);
        MaxRedeemablePoints = Math.Min(CustomerLoyaltyPoints, maxPointsForBill);
        PointsToRedeem = Math.Min(points, MaxRedeemablePoints);
        PointsToRedeemInput = PointsToRedeem.ToString("N0", CultureInfo.InvariantCulture);
        RecalculatePoints();
    }

    [RelayCommand]
    private void ClearPoints()
    {
        PointsToRedeem = 0;
        PointsToRedeemInput = "0";
        RecalculatePoints();
    }

    partial void OnPointsToRedeemInputChanged(string value)
    {
        var basePayable = Math.Max(0, SubTotal - VoucherDiscount);
        var maxPointsForBill = (decimal)Math.Ceiling((double)basePayable / 1000.0);
        MaxRedeemablePoints = Math.Min(CustomerLoyaltyPoints, maxPointsForBill);

        if (string.IsNullOrWhiteSpace(value))
        {
            PointsToRedeem = 0;
            RecalculatePoints();
            return;
        }

        var clean = Regex.Replace(value, @"[^\d]", "");
        if (decimal.TryParse(clean, out var parsed))
        {
            PointsToRedeem = Math.Min(parsed, MaxRedeemablePoints);
            RecalculatePoints();
        }
    }

    public void RecalculatePoints()
    {
        var basePayable = Math.Max(0, SubTotal - VoucherDiscount);
        var maxPointsForBill = (decimal)Math.Ceiling((double)basePayable / 1000.0);
        MaxRedeemablePoints = Math.Min(CustomerLoyaltyPoints, maxPointsForBill);

        if (PointsToRedeem > MaxRedeemablePoints)
        {
            PointsToRedeem = MaxRedeemablePoints;
            PointsToRedeemInput = PointsToRedeem.ToString("N0", CultureInfo.InvariantCulture);
        }

        // 1 điểm = 1.000 VNĐ theo Backend PointRedemptionRate
        PointsDiscountAmount = Math.Min(PointsToRedeem * 1000m, basePayable);
        PointsRemainingAfter = Math.Max(0, CustomerLoyaltyPoints - (int)PointsToRedeem);

        // Cập nhật tổng tiền cần thanh toán
        GrandTotal = Math.Max(0, basePayable - PointsDiscountAmount);

        OnPropertyChanged(nameof(HasPointsDiscount));
        OnPropertyChanged(nameof(HasCustomerPoints));

        if (!IsSplitPayment)
        {
            if (SelectedPaymentMethod == "Cash")
            {
                TenderedCash = GrandTotal;
                TenderedCashInput = GrandTotal.ToString("N0", CultureInfo.InvariantCulture);
                ChangeAmount = 0;
            }
            else if (SelectedPaymentMethod == "VietQR" || SelectedPaymentMethod == "MoMo")
            {
                GenerateDynamicQrs();
            }
        }
        else
        {
            RecalculateSplitTotals();
        }

        SyncPaymentToCfd();
    }

    // ── Cash Calculations ──
    partial void OnTenderedCashInputChanged(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            TenderedCash = 0;
            ChangeAmount = 0;
            SyncPaymentToCfd();
            return;
        }

        var cleanDigits = Regex.Replace(value, @"[^\d]", "");
        if (decimal.TryParse(cleanDigits, out var parsed))
        {
            TenderedCash = parsed;

            if (IsSplitPayment)
            {
                var cashItem = SplitPayments.FirstOrDefault(p => p.IsCash);
                if (cashItem != null)
                {
                    cashItem.TenderedCash = parsed;
                    ChangeAmount = Math.Max(0, parsed - cashItem.Amount);
                }
            }
            else
            {
                ChangeAmount = Math.Max(0, TenderedCash - GrandTotal);
            }
        }
        else
        {
            TenderedCash = 0;
            ChangeAmount = 0;
        }

        SyncPaymentToCfd();
    }

    [RelayCommand]
    private void AddQuickCash(object? param)
    {
        if (param == null) return;

        decimal addAmount = 0;
        if (param is int iVal) addAmount = iVal;
        else if (param is long lVal) addAmount = lVal;
        else if (param is decimal dVal) addAmount = dVal;
        else if (decimal.TryParse(param.ToString(), out var pVal)) addAmount = pVal;

        if (addAmount > 0)
        {
            TenderedCash += addAmount;
            TenderedCashInput = TenderedCash.ToString("N0", CultureInfo.InvariantCulture);

            if (IsSplitPayment)
            {
                var cashItem = SplitPayments.FirstOrDefault(p => p.IsCash);
                if (cashItem != null)
                {
                    cashItem.TenderedCash = TenderedCash;
                    ChangeAmount = Math.Max(0, TenderedCash - cashItem.Amount);
                }
            }
            else
            {
                ChangeAmount = Math.Max(0, TenderedCash - GrandTotal);
            }

            SyncPaymentToCfd();
        }
    }

    [RelayCommand]
    private void SetExactCash()
    {
        if (IsSplitPayment)
        {
            var cashItem = SplitPayments.FirstOrDefault(p => p.IsCash);
            if (cashItem != null)
            {
                TenderedCash = cashItem.Amount;
                TenderedCashInput = cashItem.Amount.ToString("N0", CultureInfo.InvariantCulture);
                ChangeAmount = 0;
            }
        }
        else
        {
            TenderedCash = GrandTotal;
            TenderedCashInput = GrandTotal.ToString("N0", CultureInfo.InvariantCulture);
            ChangeAmount = 0;
        }

        SyncPaymentToCfd();
    }
}
