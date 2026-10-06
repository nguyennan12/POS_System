using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace POS.WinUI.ViewModels.Cashier;

public partial class PosCashierMainViewModel
{
    // ════════════════════ VIETQR PAYMENT ════════════════════

    [ObservableProperty]
    private string _vietQrBankName = "MB Bank";

    [ObservableProperty]
    private string _vietQrAccountNo = "0988888888";

    [ObservableProperty]
    private string _vietQrAccountName = "CỬA HÀNG POS";

    [ObservableProperty]
    private string _vietQrTransferContent = "THANHTOAN";

    [ObservableProperty]
    private string _vietQrStatus = "Đang chờ khách quét mã...";

    [ObservableProperty]
    private System.Windows.Media.Imaging.BitmapSource? _vietQrCodeImage;

    [RelayCommand]
    private async Task RefreshVietQrAsync()
    {
        VietQrStatus = "Đang làm mới mã QR...";
        await GenerateDynamicQrsAsync();
        VietQrStatus = "Đang chờ khách quét mã...";
    }

    [RelayCommand]
    private void ConfirmVietQrPaid()
    {
        VietQrStatus = "✓ Đã nhận thanh toán chuyển khoản";
        ShowSuccess("Đã xác nhận nhận tiền chuyển khoản VietQR thành công!");
        SyncPaymentToCfd(isCompleted: true);
    }

    public async Task GenerateDynamicQrsAsync()
    {
        try
        {
            VietQrTransferContent = $"HD{DateTime.Now:HHmmss}";
            var content = VietQrTransferContent;
            var bankName = VietQrBankName;
            var accountNo = VietQrAccountNo;
            var accountName = VietQrAccountName;
            var amount = (long)GrandTotal;
            var method = SelectedPaymentMethod;

            var (vQr, mQr) = await Task.Run(() =>
            {
                System.Windows.Media.Imaging.BitmapSource? v = null;
                System.Windows.Media.Imaging.BitmapSource? m = null;

                if (method == "VietQR" || string.IsNullOrEmpty(method))
                {
                    var qrPayload = $"https://img.vietqr.io/image/{bankName}-{accountNo}-compact2.png?amount={amount}&addInfo={content}&accountName={Uri.EscapeDataString(accountName)}";
                    v = GenerateQrBitmap(qrPayload, 200);
                }

                if (method == "MoMo" || string.IsNullOrEmpty(method))
                {
                    var momoPayload = $"2|99|{accountNo}|{accountName}|pos@store.vn|0|0|{amount}|{content}|transfer_myqr";
                    m = GenerateQrBitmap(momoPayload, 200);
                }

                return (v, m);
            });

            if (vQr != null) VietQrCodeImage = vQr;
            if (mQr != null) MoMoQrCodeImage = mQr;
            SyncPaymentToCfd();
        }
        catch
        {
            // Bỏ qua lỗi tạo QR
        }
    }

    public void GenerateDynamicQrs()
    {
        _ = GenerateDynamicQrsAsync();
    }

    public static System.Windows.Media.Imaging.BitmapSource? GenerateQrBitmap(string content, int size = 200)
    {
        try
        {
            var writer = new ZXing.BarcodeWriterPixelData
            {
                Format = ZXing.BarcodeFormat.QR_CODE,
                Options = new ZXing.QrCode.QrCodeEncodingOptions
                {
                    Height = size,
                    Width = size,
                    Margin = 1
                }
            };
            var pixelData = writer.Write(content);
            var bmp = System.Windows.Media.Imaging.BitmapSource.Create(
                pixelData.Width,
                pixelData.Height,
                96,
                96,
                System.Windows.Media.PixelFormats.Bgra32,
                null,
                pixelData.Pixels,
                pixelData.Width * 4);

            bmp.Freeze(); // Cho phép cross-thread rendering và GPU render mượt mà
            return bmp;
        }
        catch
        {
            return null;
        }
    }
}
