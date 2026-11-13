using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace POS.WinUI.ViewModels.Settings;

public sealed partial class SettingsViewModel
{
    // ── Thiết lập máy in nhiệt ───────────────────────────────────────
    [ObservableProperty]
    private string _printerModel = "K80";

    [ObservableProperty]
    private bool _isK80 = true;

    [ObservableProperty]
    private bool _isK58;

    [ObservableProperty]
    private double _previewWidth = 380;

    [ObservableProperty]
    private string _selectedPort = "USB (COM3 - Máy in nhiệt ESC/POS)";

    public ObservableCollection<string> AvailablePorts { get; } =
    [
        "USB (COM3 - Máy in nhiệt ESC/POS)",
        "USB (COM1)",
        "Mạng LAN (192.168.1.200:9100)",
        "Máy in mặc định Windows (Default POS Printer)"
    ];

    [ObservableProperty]
    private bool _autoPrintReceipt = true;

    [ObservableProperty]
    private int _printCopies = 1;

    public ObservableCollection<int> AvailableCopies { get; } = [1, 2, 3];

    // ── Tiêu đề và Chân trang hóa đơn ────────────────────────────────
    [ObservableProperty]
    private string _receiptHeader = "CỬA HÀNG TRUNG TÂM\n123 Đường Nguyễn Huệ, Quận 1, TP. Hồ Chí Minh\nHotline: 0901 234 567\nChào mừng quý khách đến với OraPOS!";

    [ObservableProperty]
    private string _receiptFooter = "Cảm ơn và hẹn gặp lại quý khách!\nChúc quý khách một ngày tốt lành! 😊";

    [ObservableProperty]
    private int _headerCharCount;

    [ObservableProperty]
    private int _footerCharCount;

    partial void OnReceiptHeaderChanged(string value)
    {
        HeaderCharCount = value?.Length ?? 0;
    }

    partial void OnReceiptFooterChanged(string value)
    {
        FooterCharCount = value?.Length ?? 0;
    }

    // ── Mã QR WiFi trên hóa đơn ───────────────────────────────────────
    [ObservableProperty]
    private string _wifiSsid = "OraPOS_Store_WiFi";

    [ObservableProperty]
    private string _wifiPassword = "matkhaucuahang";

    [ObservableProperty]
    private bool _showWifiPassword;

    private void InitializePrinterSettings()
    {
        HeaderCharCount = ReceiptHeader.Length;
        FooterCharCount = ReceiptFooter.Length;
        ApplyPrinterModel("K80");
    }

    private void ApplyPrinterModel(string model)
    {
        PrinterModel = model;
        if (model == "K58")
        {
            IsK58 = true;
            IsK80 = false;
            PreviewWidth = 300;
        }
        else
        {
            IsK80 = true;
            IsK58 = false;
            PreviewWidth = 380;
        }
    }

    [RelayCommand]
    private void SelectK80()
    {
        ApplyPrinterModel("K80");
    }

    [RelayCommand]
    private void SelectK58()
    {
        ApplyPrinterModel("K58");
    }

    [RelayCommand]
    private void RefreshPorts()
    {
        ShowInfo("Đã quét lại danh sách cổng USB/COM và máy in kết nối.");
    }

    [RelayCommand]
    private void ToggleWifiPassword()
    {
        ShowWifiPassword = !ShowWifiPassword;
    }

    [RelayCommand]
    private void GenerateWifiQr()
    {
        if (string.IsNullOrWhiteSpace(WifiSsid))
        {
            ShowWarning("Vui lòng nhập tên WiFi trước khi tạo mã QR.");
            return;
        }
        ShowSuccess($"Đã cập nhật mã QR WiFi cho mạng '{WifiSsid}' trên hóa đơn!");
    }
}
