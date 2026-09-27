using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using POS.WinUI.ApiClients;
using POS.WinUI.Models;
using POS.WinUI.Services;

namespace POS.WinUI.ViewModels.Auth;

public partial class LoginViewModel : ObservableObject
{
    private readonly SessionService _sessionService;
    private readonly AuthApiClient _authApiClient;
    private readonly StoreApiClient _storeApiClient;
    private readonly NetworkStatusService _networkStatusService;

    // ── Network ───────────────────────────────────────────────────
    [ObservableProperty] private bool _isServerConnected;
    [ObservableProperty] private string _serverStatusText = string.Empty;

    // ── Stores ────────────────────────────────────────────────────
    [ObservableProperty] private ObservableCollection<StoreItem> _stores = new();
    [ObservableProperty] private StoreItem? _selectedStore;

    // ── Login form ────────────────────────────────────────────────
    [ObservableProperty] private string _username = string.Empty;
    [ObservableProperty] private string _password = string.Empty;
    [ObservableProperty] private bool _rememberMe = true;

    // ── PIN ───────────────────────────────────────────────────────
    [ObservableProperty] private string _pin = string.Empty;
    [ObservableProperty] private bool _isPinMode = false;

    /// <summary>True khi đang ở tab Mật khẩu — dùng để set Tag trên SegmentedTabButton.</summary>
    public bool IsPasswordMode => !IsPinMode;

    /// <summary>Số ký tự PIN đã nhập — dùng để drive PinInput.PinLength DP.</summary>
    [ObservableProperty] private int _pinLength;

    /// <summary>Cho phép submit khi đủ 6 số và không đang loading.</summary>
    public bool CanSubmitPin => Pin.Length == 6 && !IsLoading;

    // ── State ─────────────────────────────────────────────────────
    [ObservableProperty] private bool _isLoading = false;
    [ObservableProperty] private string? _errorMessage;
    [ObservableProperty] private string? _successMessage;

    public LoginViewModel(
        SessionService sessionService,
        AuthApiClient authApiClient,
        StoreApiClient storeApiClient,
        NetworkStatusService networkStatusService)
    {
        _sessionService = sessionService;
        _authApiClient = authApiClient;
        _storeApiClient = storeApiClient;
        _networkStatusService = networkStatusService;

        _isServerConnected = _networkStatusService.IsOnline;
        _serverStatusText = _networkStatusService.StatusText;

        _networkStatusService.StatusChanged += OnNetworkStatusChanged;

        if (_networkStatusService.IsOnline)
            _ = LoadStoresAsync();
    }

    private void OnNetworkStatusChanged(bool isOnline)
    {
        IsServerConnected = isOnline;
        ServerStatusText = _networkStatusService.StatusText;

        if (isOnline && Stores.Count == 0)
            _ = LoadStoresAsync();
    }

    [RelayCommand]
    public async Task CheckConnectionAndLoadStoresAsync()
    {
        ServerStatusText = "Đang kiểm tra kết nối...";
        bool isHealthy = await _networkStatusService.CheckHealthAsync();
        IsServerConnected = isHealthy;
        ServerStatusText = _networkStatusService.StatusText;

        if (isHealthy && Stores.Count == 0)
            await LoadStoresAsync();
    }

    private async Task LoadStoresAsync()
    {
        try
        {
            var res = await _storeApiClient.GetPublicStoresAsync();
            if (res?.Success == true && res.Data != null)
            {
                Stores.Clear();
                foreach (var s in res.Data)
                    Stores.Add(new StoreItem { Id = s.Id, Name = s.Name });

                if (Stores.Count > 0)
                    SelectedStore = Stores[0];
            }
        }
        catch
        {
            // NetworkStatusHandler đã tự động bắt lỗi và gọi ReportFailure()
        }
    }

    // ── PIN input handlers ────────────────────────────────────────

    partial void OnPinChanged(string value)
    {
        PinLength = value.Length;
        OnPropertyChanged(nameof(CanSubmitPin));

        // Tự động đăng nhập khi nhập đủ 6 số PIN
        if (value.Length == 6 && !IsLoading)
            _ = LoginPinAsync();
    }

    [RelayCommand]
    private void SwitchToPassword()
    {
        IsPinMode = false;
        ErrorMessage = null;
    }

    [RelayCommand]
    private void SwitchToPin()
    {
        IsPinMode = true;
        ErrorMessage = null;
    }

    partial void OnIsPinModeChanged(bool value) =>
        OnPropertyChanged(nameof(IsPasswordMode));

    [RelayCommand]
    private void AddPin(string digit)
    {
        if (IsLoading || Pin.Length >= 6) return;
        ErrorMessage = null;
        Pin += digit;
    }

    [RelayCommand]
    private void BackspacePin()
    {
        if (IsLoading || Pin.Length == 0) return;
        ErrorMessage = null;
        Pin = Pin[..^1];
    }

    [RelayCommand]
    private void ClearPin()
    {
        if (IsLoading) return;
        ErrorMessage = null;
        Pin = string.Empty;
    }

    // ── Toast helper ──────────────────────────────────────────────

    private CancellationTokenSource? _toastCts;

    private async Task ShowSuccessToastAsync(string message, int durationMs = 3000)
    {
        _toastCts?.Cancel();
        _toastCts = new CancellationTokenSource();
        var token = _toastCts.Token;

        SuccessMessage = message;
        ErrorMessage = null;

        try
        {
            await Task.Delay(durationMs, token);
            if (!token.IsCancellationRequested)
                SuccessMessage = null;
        }
        catch (TaskCanceledException)
        {
            // Ignore — another toast was triggered or CTS was disposed
        }
    }

    // ── Login commands ────────────────────────────────────────────

    [RelayCommand]
    private async Task LoginPasswordAsync()
    {
        if (IsLoading) return;
        ErrorMessage = null;

        if (string.IsNullOrWhiteSpace(Username))
        {
            ErrorMessage = "Vui lòng nhập tên đăng nhập.";
            return;
        }

        if (string.IsNullOrEmpty(Password))
        {
            ErrorMessage = "Vui lòng nhập mật khẩu.";
            return;
        }

        IsLoading = true;
        try
        {
            var response = await _authApiClient.LoginWithPasswordAsync(Username.Trim(), Password);
            if (response?.Success == true && response.Data != null)
            {
                var auth = response.Data;
                _sessionService.SetSession(
                    auth.AccessToken,
                    auth.RefreshToken,
                    auth.User.Id.ToString(),
                    auth.User.Name,
                    auth.User.RoleName,
                    auth.User.StoreId?.ToString() ?? (SelectedStore?.Id.ToString() ?? string.Empty));

                _ = ShowSuccessToastAsync("Đăng nhập thành công!", 3000);
            }
            else
            {
                ErrorMessage = response?.Error?.Message ?? response?.Message ?? "Đăng nhập không thành công.";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task LoginPinAsync()
    {
        if (IsLoading) return;

        if (SelectedStore == null)
        {
            ErrorMessage = "Vui lòng chọn chi nhánh trước khi đăng nhập bằng PIN.";
            Pin = string.Empty;
            return;
        }

        if (Pin.Length != 6)
        {
            ErrorMessage = "Vui lòng nhập đủ 6 số mã PIN.";
            return;
        }

        ErrorMessage = null;
        IsLoading = true;
        try
        {
            var response = await _authApiClient.LoginWithPinAsync(SelectedStore.Id, Pin);
            if (response?.Success == true && response.Data != null)
            {
                var auth = response.Data;
                _sessionService.SetSession(
                    auth.AccessToken,
                    auth.RefreshToken,
                    auth.User.Id.ToString(),
                    auth.User.Name,
                    auth.User.RoleName,
                    auth.User.StoreId?.ToString() ?? SelectedStore.Id.ToString());

                _ = ShowSuccessToastAsync("Đăng nhập thành công!", 3000);
            }
            else
            {
                ErrorMessage = response?.Error?.Message ?? response?.Message ?? "Mã PIN không chính xác.";
                Pin = string.Empty;
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            Pin = string.Empty;
        }
        finally
        {
            IsLoading = false;
        }
    }
}
