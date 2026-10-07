using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace POS.WinUI.ViewModels.Cashier;

public partial class PosCashierMainViewModel
{
    // ════════════════════ MOMO E-WALLET PAYMENT ════════════════════

    [ObservableProperty]
    private int _moMoMode = 0; // 0: Khách quét QR, 1: Thu ngân quét barcode

    [ObservableProperty]
    private string _moMoBarcodeScanInput = string.Empty;

    [ObservableProperty]
    private string _moMoStatus = "Đang chờ quét mã ví MoMo...";

    [ObservableProperty]
    private System.Windows.Media.Imaging.BitmapSource? _moMoQrCodeImage;

    [RelayCommand]
    private async Task RefreshMoMoAsync()
    {
        MoMoStatus = "Đang làm mới mã QR...";
        await GenerateDynamicQrsAsync();
        MoMoStatus = "Đang chờ quét mã ví MoMo...";
    }

    [RelayCommand]
    private void ConfirmMoMoPaid()
    {
        MoMoStatus = "✓ Đã nhận thanh toán ví MoMo";
        ShowSuccess("Đã xác nhận nhận tiền ví MoMo thành công!");
        SyncPaymentToCfd(isCompleted: true);
    }

    [RelayCommand]
    private void SwitchMoMoMode(object? param)
    {
        int mode = 0;
        if (param is int i) mode = i;
        else if (param != null && int.TryParse(param.ToString(), out var pInt)) mode = pInt;

        MoMoMode = mode;
        if (mode == 1)
        {
            MoMoStatus = "Vui lòng quét mã thanh toán trên app của khách";
        }
        else
        {
            MoMoStatus = "Khách đang quét mã QR...";
        }
        SyncPaymentToCfd();
    }

    [RelayCommand]
    private void ProcessMoMoBarcodeScan()
    {
        if (string.IsNullOrWhiteSpace(MoMoBarcodeScanInput)) return;

        MoMoStatus = "✓ Đã quét mã ví MoMo thành công";
        ShowSuccess($"Đã nhận mã MoMo: {MoMoBarcodeScanInput}");
        SyncPaymentToCfd(isCompleted: true);
    }
}
