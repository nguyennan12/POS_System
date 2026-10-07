using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using POS.Contracts.V1.Registers;
using POS.Contracts.V1.Shifts;

namespace POS.WinUI.ViewModels.Cashier;

public partial class PosCashierMainViewModel
{
    // ════════════════════ REGISTERS (QUẦY / MÁY POS) ════════════════════
    public ObservableCollection<RegisterResponse> Registers { get; } = new();

    [ObservableProperty]
    private RegisterResponse? _selectedRegister;

    [ObservableProperty]
    private string _registerName = "Quầy 01";

    [ObservableProperty]
    private bool _isLoadingRegisters = false;

    [ObservableProperty]
    private bool _canChangeRegister = false;

    [ObservableProperty]
    private bool _isCashierRole = false;

    // ════════════════════ SHIFT STATE & SUMMARY ════════════════════
    [ObservableProperty]
    private ShiftSummaryResponse? _currentShiftSummary;

    // ════════════════════ SHIFT MODAL DIALOG ════════════════════
    [ObservableProperty]
    private bool _isShiftModalOpen = false;

    /// <summary>
    /// "Open" = Mở ca mới, "Close" = Đóng ca & kiểm kê tiền mặt, "Details" = Xem chi tiết ca
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsShiftModalOpenMode))]
    [NotifyPropertyChangedFor(nameof(IsShiftModalCloseMode))]
    private string _shiftModalMode = "Open";

    public bool IsShiftModalOpenMode => string.Equals(ShiftModalMode, "Open", StringComparison.OrdinalIgnoreCase);
    public bool IsShiftModalCloseMode => string.Equals(ShiftModalMode, "Close", StringComparison.OrdinalIgnoreCase);

    [ObservableProperty]
    private string _openingCashInput = "0";

    [ObservableProperty]
    private string? _openShiftNote;

    [ObservableProperty]
    private string _closingActualCashInput = "0";

    [ObservableProperty]
    private string? _closeShiftNote;

    [ObservableProperty]
    private decimal _closingExpectedCash = 0;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsClosingDifferenceNegative))]
    private decimal _closingDifference = 0;

    public bool IsClosingDifferenceNegative => ClosingDifference < 0;

    [ObservableProperty]
    private bool _isShiftProcessing = false;

    [ObservableProperty]
    private string? _shiftModalErrorMessage;

    partial void OnClosingActualCashInputChanged(string value)
    {
        RecalculateShiftDifference();
    }

    private void RecalculateShiftDifference()
    {
        var cleaned = new string(ClosingActualCashInput.Where(char.IsDigit).ToArray());
        if (decimal.TryParse(cleaned, NumberStyles.Any, CultureInfo.InvariantCulture, out var actual))
        {
            ClosingDifference = actual - ClosingExpectedCash;
        }
        else
        {
            ClosingDifference = -ClosingExpectedCash;
        }
    }

    public async Task LoadRegistersAndShiftAsync()
    {
        if (string.IsNullOrWhiteSpace(_sessionService.StoreId) || !Guid.TryParse(_sessionService.StoreId, out var storeId))
        {
            return;
        }

        IsLoadingRegisters = true;
        try
        {
            var res = await _registerApiClient.GetRegistersByStoreAsync(storeId);
            Registers.Clear();
            if (res?.Success == true && res.Data != null && res.Data.Count > 0)
            {
                foreach (var reg in res.Data)
                {
                    Registers.Add(reg);
                }

                RegisterResponse? matched = null;
                // 1. Kiểm tra cấu hình quầy đã gán cố định cho máy tính này từ trước
                var savedDeviceRegId = _localDeviceSettingsService.SavedRegisterId;
                if (!string.IsNullOrWhiteSpace(savedDeviceRegId) && Guid.TryParse(savedDeviceRegId, out var devRegGuid))
                {
                    matched = Registers.FirstOrDefault(r => r.Id == devRegGuid);
                }

                // 2. Nếu chưa có cấu hình máy, kiểm tra session
                if (matched == null && !string.IsNullOrWhiteSpace(_sessionService.RegisterId) && Guid.TryParse(_sessionService.RegisterId, out var sessionRegGuid))
                {
                    matched = Registers.FirstOrDefault(r => r.Id == sessionRegGuid);
                }

                // 3. Nếu chưa từng gán quầy nào, tự động gán Quầy mặc định đầu tiên (Quầy 01)
                if (matched == null)
                {
                    matched = Registers[0];
                }

                SelectedRegister = matched;
                RegisterName = matched.Name;
                _sessionService.SetRegister(matched.Id.ToString(), matched.Name);
                _localDeviceSettingsService.SaveDeviceBinding(storeId.ToString(), StoreName, matched.Id.ToString(), matched.Name);
            }
            else
            {
                RegisterName = "Quầy 01";
            }
        }
        catch
        {
            RegisterName = "Quầy 01";
        }
        finally
        {
            IsLoadingRegisters = false;
        }

        await LoadCurrentShiftAsync();
    }

    partial void OnSelectedRegisterChanged(RegisterResponse? value)
    {
        if (value != null)
        {
            RegisterName = value.Name;
            _sessionService.SetRegister(value.Id.ToString(), value.Name);
            _localDeviceSettingsService.SaveRegisterBinding(value.Id.ToString(), value.Name);
            _ = LoadCurrentShiftAsync();
        }
    }

    [RelayCommand]
    public async Task LoadCurrentShiftAsync()
    {
        if (string.IsNullOrWhiteSpace(_sessionService.StoreId) || !Guid.TryParse(_sessionService.StoreId, out var storeId))
        {
            ShiftName = "Chưa mở ca";
            CurrentShiftId = null;
            CurrentShiftSummary = null;
            return;
        }

        Guid? regId = null;
        if (SelectedRegister != null)
        {
            regId = SelectedRegister.Id;
        }
        else if (!string.IsNullOrWhiteSpace(_sessionService.RegisterId) && Guid.TryParse(_sessionService.RegisterId, out var sRegId))
        {
            regId = sRegId;
        }

        try
        {
            var res = await _shiftApiClient.GetCurrentShiftAsync(storeId, regId);
            if (res?.Success == true && res.Data != null)
            {
                var shift = res.Data;
                CurrentShiftId = shift.ShiftId;
                CurrentShiftSummary = shift;
                ShiftName = "Đang trong ca";
            }
            else
            {
                ShiftName = "Chưa mở ca";
                CurrentShiftId = null;
                CurrentShiftSummary = null;
            }
        }
        catch
        {
            ShiftName = "Chưa mở ca";
            CurrentShiftId = null;
            CurrentShiftSummary = null;
        }
    }

    [RelayCommand]
    public void OpenShiftModal()
    {
        ShiftModalErrorMessage = null;
        if (HasOpenShift && CurrentShiftSummary != null)
        {
            // Mở modal Đóng ca / Kiểm kê
            ShiftModalMode = "Close";
            ClosingExpectedCash = CurrentShiftSummary.ExpectedCash;
            ClosingActualCashInput = CurrentShiftSummary.ExpectedCash.ToString("N0", CultureInfo.InvariantCulture);
            CloseShiftNote = string.Empty;
            RecalculateShiftDifference();
        }
        else
        {
            // Mở modal Mở ca mới
            ShiftModalMode = "Open";
            OpeningCashInput = "0";
            OpenShiftNote = string.Empty;
            if (Registers.Count == 0)
            {
                _ = LoadRegistersAndShiftAsync();
            }
        }
        IsShiftModalOpen = true;
    }

    [RelayCommand]
    public void CloseShiftModal()
    {
        IsShiftModalOpen = false;
        ShiftModalErrorMessage = null;
    }

    [RelayCommand]
    private void SetQuickOpeningCash(string amountStr)
    {
        if (decimal.TryParse(amountStr, out var amount))
        {
            OpeningCashInput = amount.ToString("N0", CultureInfo.InvariantCulture);
        }
    }

    [RelayCommand]
    private void SetQuickClosingCash(string amountStr)
    {
        if (decimal.TryParse(amountStr, out var amount))
        {
            ClosingActualCashInput = amount.ToString("N0", CultureInfo.InvariantCulture);
            RecalculateShiftDifference();
        }
    }

    [RelayCommand]
    private async Task SubmitOpenShiftAsync()
    {
        if (IsShiftProcessing) return;

        if (string.IsNullOrWhiteSpace(_sessionService.StoreId) || !Guid.TryParse(_sessionService.StoreId, out var storeId))
        {
            ShiftModalErrorMessage = "Vui lòng chọn cửa hàng hợp lệ.";
            return;
        }

        if (SelectedRegister == null)
        {
            ShiftModalErrorMessage = "Vui lòng chọn quầy thu ngân để mở ca.";
            return;
        }

        var cleaned = new string(OpeningCashInput.Where(char.IsDigit).ToArray());
        if (!decimal.TryParse(cleaned, NumberStyles.Any, CultureInfo.InvariantCulture, out var openingCash) || openingCash < 0)
        {
            ShiftModalErrorMessage = "Số tiền mặt đầu ca không hợp lệ.";
            return;
        }

        ShiftModalErrorMessage = null;
        IsShiftProcessing = true;
        try
        {
            var req = new OpenShiftRequest(storeId, SelectedRegister.Id, openingCash, OpenShiftNote);
            var res = await _shiftApiClient.OpenShiftAsync(req);

            if (res?.Success == true && res.Data != null)
            {
                // Lưu vĩnh viễn cấu hình quầy đã chọn vào máy tính
                _localDeviceSettingsService.SaveDeviceBinding(storeId.ToString(), StoreName, SelectedRegister.Id.ToString(), SelectedRegister.Name);
                _sessionService.SetRegister(SelectedRegister.Id.ToString(), SelectedRegister.Name);
                RegisterName = SelectedRegister.Name;

                IsShiftModalOpen = false;
                ShowSuccess($"Mở ca thành công cho {SelectedRegister.Name}!");
                await LoadCurrentShiftAsync();
            }
            else
            {
                ShiftModalErrorMessage = res?.Error?.Message ?? res?.Message ?? "Không thể mở ca làm việc.";
            }
        }
        catch (Exception ex)
        {
            ShiftModalErrorMessage = ex.Message;
        }
        finally
        {
            IsShiftProcessing = false;
        }
    }

    [RelayCommand]
    private async Task SubmitCloseShiftAsync()
    {
        if (IsShiftProcessing || CurrentShiftId == null) return;

        var cleaned = new string(ClosingActualCashInput.Where(char.IsDigit).ToArray());
        if (!decimal.TryParse(cleaned, NumberStyles.Any, CultureInfo.InvariantCulture, out var actualCash) || actualCash < 0)
        {
            ShiftModalErrorMessage = "Số tiền mặt thực tế kiểm đếm không hợp lệ.";
            return;
        }

        ShiftModalErrorMessage = null;
        IsShiftProcessing = true;
        try
        {
            var req = new CloseShiftRequest(actualCash, CloseShiftNote);
            var res = await _shiftApiClient.CloseShiftAsync(CurrentShiftId.Value, req);

            if (res?.Success == true && res.Data != null)
            {
                IsShiftModalOpen = false;
                ShowSuccess($"Đóng ca thành công cho {RegisterName}!");
                await LoadCurrentShiftAsync();
            }
            else
            {
                ShiftModalErrorMessage = res?.Error?.Message ?? res?.Message ?? "Không thể đóng ca làm việc.";
            }
        }
        catch (Exception ex)
        {
            ShiftModalErrorMessage = ex.Message;
        }
        finally
        {
            IsShiftProcessing = false;
        }
    }
}
