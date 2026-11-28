using System;
using System.Threading.Tasks;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;

namespace POS.WinUI.ViewModels.Cashier;

public partial class PosCashierMainViewModel
{
    private readonly DispatcherTimer _phoneScannerTimer = new() { Interval = TimeSpan.FromSeconds(1) };
    private Guid? _phoneScannerReceiverId;
    private Guid? _phoneScannerShiftId;
    private bool _phoneScannerPolling;
    private int _phoneScannerVersion;

    [ObservableProperty] private bool _isPhoneScannerConnected;

    partial void OnCurrentShiftIdChanged(Guid? value)
    {
        if (value.HasValue) _ = AttachPhoneScannerAsync(value.Value);
        else _ = StopPhoneScannerAsync();
    }

    private async Task AttachPhoneScannerAsync(Guid shiftId)
    {
        await StopPhoneScannerAsync();
        var version = _phoneScannerVersion;
        try
        {
            var receiver = await _cashierScannerApiClient.StartReceiverAsync(shiftId);
            if (receiver is null) throw new InvalidOperationException("Máy chủ không trả về đầu nhận mã.");
            if (version != _phoneScannerVersion || CurrentShiftId != shiftId)
            {
                await _cashierScannerApiClient.StopReceiverAsync(receiver.ReceiverId, shiftId);
                return;
            }
            _phoneScannerReceiverId = receiver.ReceiverId;
            _phoneScannerShiftId = shiftId;
            IsPhoneScannerConnected = true;
            _phoneScannerTimer.Tick -= OnPhoneScannerTick;
            _phoneScannerTimer.Tick += OnPhoneScannerTick;
            _phoneScannerTimer.Start();
        }
        catch (Exception ex)
        {
            if (version == _phoneScannerVersion)
                ShowWarning($"Máy quét điện thoại chưa sẵn sàng: {ex.Message}");
        }
    }

    private async Task StopPhoneScannerAsync()
    {
        _phoneScannerVersion++;
        _phoneScannerTimer.Stop();
        _phoneScannerTimer.Tick -= OnPhoneScannerTick;
        var receiverId = _phoneScannerReceiverId;
        var shiftId = _phoneScannerShiftId;
        _phoneScannerReceiverId = null;
        _phoneScannerShiftId = null;
        IsPhoneScannerConnected = false;
        if (receiverId.HasValue && shiftId.HasValue)
        {
            try { await _cashierScannerApiClient.StopReceiverAsync(receiverId.Value, shiftId.Value); }
            catch { /* The receiver expires after missed heartbeats. */ }
        }
    }

    private async void OnPhoneScannerTick(object? sender, EventArgs e)
    {
        if (_phoneScannerPolling || _phoneScannerReceiverId is not Guid receiverId ||
            _phoneScannerShiftId is not Guid shiftId) return;
        if (CurrentShiftId != shiftId)
        {
            await StopPhoneScannerAsync();
            return;
        }

        _phoneScannerPolling = true;
        try
        {
            var paused = IsPaymentModalOpen || IsProcessingCheckout;
            var scans = await _cashierScannerApiClient.PollAsync(receiverId, shiftId, paused);
            if (paused || scans is null) return;
            foreach (var scan in scans)
            {
                SearchQuery = scan.Barcode;
                await ScanBarcodeAsync();
            }
        }
        catch (Exception ex)
        {
            _phoneScannerTimer.Stop();
            IsPhoneScannerConnected = false;
            ShowWarning($"Mất kết nối máy quét điện thoại: {ex.Message}");
            if (CurrentShiftId == shiftId)
                _ = AttachPhoneScannerAsync(shiftId);
        }
        finally
        {
            _phoneScannerPolling = false;
        }
    }
}
