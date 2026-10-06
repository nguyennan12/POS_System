using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace POS.WinUI.ViewModels.Cashier;

public partial class PosCashierMainViewModel
{
    // ════════════════════ ƯU ĐÃI & VOUCHER ════════════════════

    [ObservableProperty]
    private string _voucherCode = string.Empty;

    [ObservableProperty]
    private decimal _voucherDiscount = 0;

    [ObservableProperty]
    private string _voucherMessage = string.Empty;

    [ObservableProperty]
    private bool _isApplyingVoucher = false;

    [RelayCommand]
    private async Task ApplyVoucherAsync(string? code)
    {
        var voucher = code ?? VoucherCode;
        if (string.IsNullOrWhiteSpace(voucher))
        {
            ShowWarning("Vui lòng nhập mã voucher");
            return;
        }

        IsApplyingVoucher = true;
        VoucherMessage = string.Empty;

        try
        {
            var res = await _voucherApiClient.ValidateVoucherAsync(voucher.Trim(), SubTotal, SelectedCustomerId);
            if (res?.Success == true && res.Data != null)
            {
                if (res.Data.IsValid)
                {
                    VoucherDiscount = res.Data.DiscountAmount;
                    VoucherMessage = $"Áp dụng voucher thành công: -{res.Data.DiscountAmount:N0} đ";
                    ShowSuccess(VoucherMessage);
                    RecalculateTotals();
                }
                else
                {
                    VoucherDiscount = 0;
                    VoucherMessage = res.Data.ErrorMessage ?? "Mã voucher không hợp lệ";
                    ShowError(VoucherMessage);
                    RecalculateTotals();
                }
            }
            else
            {
                VoucherDiscount = 0;
                VoucherMessage = "Không thể kiểm tra voucher";
                ShowError(VoucherMessage);
            }
        }
        catch (Exception ex)
        {
            VoucherDiscount = 0;
            VoucherMessage = ex.Message;
            ShowError($"Lỗi áp dụng voucher: {ex.Message}");
        }
        finally
        {
            IsApplyingVoucher = false;
        }
    }
}
