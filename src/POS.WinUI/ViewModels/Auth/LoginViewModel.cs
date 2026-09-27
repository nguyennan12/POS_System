using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using POS.WinUI.ApiClients;
using POS.WinUI.Services;

namespace POS.WinUI.ViewModels.Auth;

public class StoreItem
{
  public Guid Id { get; set; }
  public string Name { get; set; } = string.Empty;
  public string DisplayText => $"Chi nhánh: {Name}";
}

public partial class LoginViewModel : ObservableObject
{
  private readonly SessionService _sessionService;
  private readonly AuthApiClient _authApiClient;
  private readonly StoreApiClient _storeApiClient;
  private readonly NetworkStatusService _networkStatusService;

  [ObservableProperty]
  private bool _isServerConnected;

  [ObservableProperty]
  private string _serverStatusText;

  [ObservableProperty]
  private ObservableCollection<StoreItem> _stores = new();

  [ObservableProperty]
  private StoreItem? _selectedStore;

  [ObservableProperty]
  private string _username = string.Empty;

  [ObservableProperty]
  private string _password = string.Empty;

  [ObservableProperty]
  private string _pin = string.Empty;

  [ObservableProperty]
  private bool _isPinMode = false;

  [ObservableProperty]
  private bool _isPasswordShown = false;

  [ObservableProperty]
  private bool _rememberMe = true;

  [ObservableProperty]
  private bool _isLoading = false;

  [ObservableProperty]
  private string? _errorMessage;

  [ObservableProperty]
  private string? _successMessage;

  // PIN Dots State
  public bool PinDot0 => Pin.Length > 0;
  public bool PinDot1 => Pin.Length > 1;
  public bool PinDot2 => Pin.Length > 2;
  public bool PinDot3 => Pin.Length > 3;
  public bool PinDot4 => Pin.Length > 4;
  public bool PinDot5 => Pin.Length > 5;
  public bool CanSubmitPin => Pin.Length == 6 && !IsLoading;

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
    {
      _ = LoadStoresAsync();
    }
  }

  private void OnNetworkStatusChanged(bool isOnline)
  {
    IsServerConnected = isOnline;
    ServerStatusText = _networkStatusService.StatusText;

    if (isOnline && Stores.Count == 0)
    {
      _ = LoadStoresAsync();
    }
  }

  [RelayCommand]
  public async Task CheckConnectionAndLoadStoresAsync()
  {
    ServerStatusText = "Đang kiểm tra kết nối...";
    bool isHealthy = await _networkStatusService.CheckHealthAsync();
    IsServerConnected = isHealthy;
    ServerStatusText = _networkStatusService.StatusText;

    if (isHealthy && Stores.Count == 0)
    {
      await LoadStoresAsync();
    }
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
        {
          Stores.Add(new StoreItem { Id = s.Id, Name = s.Name });
        }
        if (Stores.Count > 0)
        {
          SelectedStore = Stores[0];
        }
      }
    }
    catch
    {
      // NetworkStatusHandler đã tự động bắt lỗi và gọi ReportFailure()
    }
  }

  partial void OnPinChanged(string value)
  {
    OnPropertyChanged(nameof(PinDot0));
    OnPropertyChanged(nameof(PinDot1));
    OnPropertyChanged(nameof(PinDot2));
    OnPropertyChanged(nameof(PinDot3));
    OnPropertyChanged(nameof(PinDot4));
    OnPropertyChanged(nameof(PinDot5));
    OnPropertyChanged(nameof(CanSubmitPin));

    // Tự động đăng nhập khi nhập đủ 6 số PIN
    if (value.Length == 6 && !IsLoading)
    {
      _ = LoginPinAsync();
    }
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

  [RelayCommand]
  private void TogglePasswordVisibility()
  {
    IsPasswordShown = !IsPasswordShown;
  }

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
    Pin = Pin.Substring(0, Pin.Length - 1);
  }

  [RelayCommand]
  private void ClearPin()
  {
    if (IsLoading) return;
    ErrorMessage = null;
    Pin = string.Empty;
  }

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
      {
        SuccessMessage = null;
      }
    }
    catch (TaskCanceledException)
    {
      // Ignore when another toast triggers or cancelled
    }
  }

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
      if (response != null && response.Success && response.Data != null)
      {
        var auth = response.Data;
        _sessionService.SetSession(
            auth.AccessToken,
            auth.RefreshToken,
            auth.User.Id.ToString(),
            auth.User.Name,
            auth.User.RoleName,
            auth.User.StoreId?.ToString() ?? (SelectedStore?.Id.ToString() ?? string.Empty)
        );

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
      if (response != null && response.Success && response.Data != null)
      {
        var auth = response.Data;
        _sessionService.SetSession(
            auth.AccessToken,
            auth.RefreshToken,
            auth.User.Id.ToString(),
            auth.User.Name,
            auth.User.RoleName,
            auth.User.StoreId?.ToString() ?? SelectedStore.Id.ToString()
        );

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
