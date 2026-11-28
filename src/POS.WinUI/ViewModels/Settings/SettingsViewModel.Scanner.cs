using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using POS.Contracts.V1.Registers;
using POS.WinUI.Core.ApiClients;
using POS.WinUI.Core.Services;
using POS.WinUI.ViewModels.Cashier;

namespace POS.WinUI.ViewModels.Settings;

public sealed partial class SettingsViewModel
{
    public ObservableCollection<RegisterResponse> ScannerRegisters { get; } = new();
    public ObservableCollection<ScannerDeviceResponse> ScannerDevices { get; } = new();

    [ObservableProperty] private RegisterResponse? _selectedScannerRegister;
    [ObservableProperty] private ScannerDeviceResponse? _selectedScannerDevice;
    [ObservableProperty] private string _scannerDeviceName = string.Empty;
    [ObservableProperty] private string _scannerPairingUrl = string.Empty;
    [ObservableProperty] private BitmapSource? _scannerPairingQr;
    [ObservableProperty] private bool _isScannerBusy;

    private async Task LoadScannerDevicesAsync()
    {
        if (!Guid.TryParse(CurrentStoreId, out var storeId)) return;
        IsScannerBusy = true;
        try
        {
            var registers = await _registerApi.GetRegistersByStoreAsync(storeId);
            var devices = await _scannerApi.GetDevicesAsync();
            var previousRegisterId = SelectedScannerRegister?.Id;
            ScannerRegisters.Clear();
            if (registers?.Success == true && registers.Data != null)
                foreach (var register in registers.Data) ScannerRegisters.Add(register);
            SelectedScannerRegister = ScannerRegisters.FirstOrDefault(r => r.Id == previousRegisterId)
                ?? ScannerRegisters.FirstOrDefault();

            ScannerDevices.Clear();
            if (devices != null)
                foreach (var device in devices) ScannerDevices.Add(device);
            SelectedScannerDevice = null;
        }
        catch (Exception ex)
        {
            ShowError($"Không tải được thiết bị quét: {ex.Message}");
        }
        finally { IsScannerBusy = false; }
    }

    [RelayCommand]
    private async Task CreateScannerDeviceAsync()
    {
        if (IsScannerBusy) return;
        if (SelectedScannerRegister is null || string.IsNullOrWhiteSpace(ScannerDeviceName))
        {
            ShowWarning("Chọn quầy và nhập tên điện thoại trước khi đăng ký.");
            return;
        }
        IsScannerBusy = true;
        try
        {
            var pairing = await _scannerApi.CreateDeviceAsync(SelectedScannerRegister.Id, ScannerDeviceName.Trim());
            if (pairing is null) throw new InvalidOperationException("Máy chủ không trả về URL ghép thiết bị.");
            ScannerDevices.Add(pairing.Device);
            SelectedScannerDevice = pairing.Device;
            ScannerDeviceName = string.Empty;
            ShowPairing(pairing);
            ShowSuccess("Đã đăng ký. Nhập URL vào kisledScan một lần; URL vẫn dùng được qua các ca bán.");
        }
        catch (Exception ex) { ShowError($"Không đăng ký được điện thoại: {ex.Message}"); }
        finally { IsScannerBusy = false; }
    }

    [RelayCommand]
    private async Task RotateScannerDeviceAsync()
    {
        if (IsScannerBusy || SelectedScannerDevice is null) return;
        if (!await _dialogs.ShowConfirmAsync(new ConfirmDialogOptions
        {
            Title = "Cấp lại URL quét",
            Message = $"URL cũ của {SelectedScannerDevice.Name} sẽ ngừng hoạt động ngay. Tiếp tục?",
            ConfirmText = "Cấp URL mới",
            Variant = ConfirmDialogVariant.Warning
        })) return;
        IsScannerBusy = true;
        try
        {
            var pairing = await _scannerApi.RotateDeviceAsync(SelectedScannerDevice.Id);
            if (pairing is null) throw new InvalidOperationException("Máy chủ không trả về URL mới.");
            ShowPairing(pairing);
            ShowSuccess("Đã cấp URL mới. Cập nhật URL trong kisledScan.");
        }
        catch (Exception ex) { ShowError($"Không cấp lại được URL: {ex.Message}"); }
        finally { IsScannerBusy = false; }
    }

    [RelayCommand]
    private async Task RevokeScannerDeviceAsync()
    {
        if (IsScannerBusy || SelectedScannerDevice is null) return;
        var device = SelectedScannerDevice;
        if (!await _dialogs.ShowConfirmAsync(new ConfirmDialogOptions
        {
            Title = "Thu hồi điện thoại quét",
            Message = $"Ngừng nhận mã từ {device.Name} tại {device.RegisterName}?",
            ConfirmText = "Thu hồi",
            Variant = ConfirmDialogVariant.Warning
        })) return;
        IsScannerBusy = true;
        try
        {
            await _scannerApi.RevokeDeviceAsync(device.Id);
            ScannerDevices.Remove(device);
            SelectedScannerDevice = null;
            ScannerPairingUrl = string.Empty;
            ScannerPairingQr = null;
            ShowSuccess("Đã thu hồi thiết bị. URL cũ không còn dùng được.");
        }
        catch (Exception ex) { ShowError($"Không thu hồi được thiết bị: {ex.Message}"); }
        finally { IsScannerBusy = false; }
    }

    [RelayCommand]
    private void CopyScannerUrl()
    {
        if (string.IsNullOrWhiteSpace(ScannerPairingUrl)) return;
        Clipboard.SetText(ScannerPairingUrl);
        ShowInfo("Đã sao chép URL webhook.");
    }

    private void ShowPairing(ScannerPairingResponse pairing)
    {
        ScannerPairingUrl = pairing.WebhookUrl;
        ScannerPairingQr = PosCashierMainViewModel.GenerateQrBitmap(pairing.WebhookUrl, 220);
    }
}
