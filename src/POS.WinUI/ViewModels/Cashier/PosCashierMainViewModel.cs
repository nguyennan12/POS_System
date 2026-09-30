using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using POS.WinUI.ApiClients;
using POS.WinUI.Services;
using POS.WinUI.Views.Auth;
using POS.WinUI.Views.Management;

namespace POS.WinUI.ViewModels.Cashier;

/// <summary>
/// ViewModel chính cho Màn hình Bán hàng / Quầy thu ngân (Cashier POS)
/// </summary>
public partial class PosCashierMainViewModel : ObservableObject
{
    private readonly SessionService _sessionService;
    private readonly NetworkStatusService _networkStatusService;
    private readonly INavigationService _navigationService;
    private readonly ShiftApiClient _shiftApiClient;
    private readonly StoreApiClient _storeApiClient;
    private readonly AuthApiClient _authApiClient;
    private readonly DispatcherTimer _clockTimer;

    [ObservableProperty]
    private string _storeName = "Chi nhánh Chưa chọn";

    [ObservableProperty]
    private bool _isOwnerRole = false;

    [ObservableProperty]
    private bool _isLoadingStores = false;

    [ObservableProperty]
    private POS.WinUI.Models.StoreItem? _selectedStore;

    public System.Collections.ObjectModel.ObservableCollection<POS.WinUI.Models.StoreItem> Stores { get; } = new();

    [ObservableProperty]
    private string _shiftName = "Đang kiểm tra ca...";

    [ObservableProperty]
    private string _employeeName = "Thu ngân";

    [ObservableProperty]
    private string _roleName = "Thu ngân";

    [ObservableProperty]
    private string _avatarInitials = "TN";

    [ObservableProperty]
    private string _currentLanguage = "VI";

    [ObservableProperty]
    private int _unreadNotificationCount = 0;

    [ObservableProperty]
    private bool _isConnected = true;

    [ObservableProperty]
    private string _statusText = "Đã kết nối với máy chủ";

    [ObservableProperty]
    private string _currentTime = DateTime.Now.ToString("HH:mm:ss");

    [ObservableProperty]
    private string _appVersion = "OraPOS v1.0.0";

    [ObservableProperty]
    private bool _isManagerRole = false;

    public PosCashierMainViewModel(
        SessionService sessionService,
        NetworkStatusService networkStatusService,
        INavigationService navigationService,
        ShiftApiClient shiftApiClient,
        StoreApiClient storeApiClient,
        AuthApiClient authApiClient)
    {
        _sessionService = sessionService;
        _networkStatusService = networkStatusService;
        _navigationService = navigationService;
        _shiftApiClient = shiftApiClient;
        _storeApiClient = storeApiClient;
        _authApiClient = authApiClient;

        LoadUserInfo();
        _ = LoadCurrentShiftAsync();

        if (IsOwnerRole)
        {
            _ = LoadStoresAsync();
        }

        // Đồng bộ mạng
        _isConnected = _networkStatusService.IsOnline;
        _statusText = _networkStatusService.StatusText;
        _networkStatusService.StatusChanged += (isOnline) =>
        {
            IsConnected = isOnline;
            StatusText = _networkStatusService.StatusText;
            if (isOnline)
            {
                _ = LoadCurrentShiftAsync();
                if (IsOwnerRole && Stores.Count == 0)
                {
                    _ = LoadStoresAsync();
                }
            }
        };

        // Đồng hồ thời gian thực
        _clockTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _clockTimer.Tick += (_, _) => CurrentTime = DateTime.Now.ToString("HH:mm:ss");
        _clockTimer.Start();
    }

    private void LoadUserInfo()
    {
        if (!string.IsNullOrWhiteSpace(_sessionService.EmployeeName))
        {
            EmployeeName = _sessionService.EmployeeName;
            AvatarInitials = GetInitials(EmployeeName);
        }

        if (!string.IsNullOrWhiteSpace(_sessionService.Role))
        {
            RoleName = _sessionService.Role switch
            {
                "StoreManager" => "Quản lý cửa hàng",
                "Owner" => "Chủ cửa hàng / Quản lý",
                "Cashier" => "Thu ngân",
                _ => _sessionService.Role
            };
        }

        IsManagerRole = _sessionService.IsManager;
        IsOwnerRole = _sessionService.IsOwner;

        if (!string.IsNullOrWhiteSpace(_sessionService.StoreName))
        {
            StoreName = _sessionService.StoreName;
        }
        else if (!string.IsNullOrWhiteSpace(_sessionService.StoreId))
        {
            StoreName = "Chi nhánh mặc định";
        }
    }

    public async Task LoadStoresAsync()
    {
        if (!IsOwnerRole) return;
        IsLoadingStores = true;
        try
        {
            var res = await _storeApiClient.GetPublicStoresAsync();
            if (res?.Success == true && res.Data != null)
            {
                Stores.Clear();
                POS.WinUI.Models.StoreItem? currentSelected = null;
                foreach (var s in res.Data)
                {
                    var item = new POS.WinUI.Models.StoreItem { Id = s.Id, Name = s.Name };
                    Stores.Add(item);
                    if (s.Id.ToString().Equals(_sessionService.StoreId, StringComparison.OrdinalIgnoreCase))
                    {
                        currentSelected = item;
                    }
                }

                if (currentSelected != null)
                {
                    SelectedStore = currentSelected;
                }
                else if (Stores.Count > 0)
                {
                    SelectedStore = Stores[0];
                }
            }
        }
        catch
        {
            // Ignore
        }
        finally
        {
            IsLoadingStores = false;
        }
    }

    partial void OnSelectedStoreChanged(POS.WinUI.Models.StoreItem? value)
    {
        if (value == null) return;
        if (string.Equals(_sessionService.StoreId, value.Id.ToString(), StringComparison.OrdinalIgnoreCase)
            && string.Equals(StoreName, value.Name, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        _sessionService.SetStore(value.Id.ToString(), value.Name);
        StoreName = value.Name;
        _ = LoadCurrentShiftAsync();
    }

    private static string GetInitials(string fullName)
    {
        var clean = System.Text.RegularExpressions.Regex.Replace(fullName, @"[^\p{L}\s]", "").Trim();
        var parts = clean.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return "POS";
        if (parts.Length == 1) return parts[0][..Math.Min(2, parts[0].Length)].ToUpperInvariant();
        return $"{char.ToUpperInvariant(parts[0][0])}{char.ToUpperInvariant(parts[^1][0])}";
    }

    [RelayCommand]
    public async Task LoadCurrentShiftAsync()
    {
        if (string.IsNullOrWhiteSpace(_sessionService.StoreId) || !Guid.TryParse(_sessionService.StoreId, out var storeId))
        {
            ShiftName = "Chưa mở ca làm việc";
            return;
        }

        try
        {
            var res = await _shiftApiClient.GetCurrentShiftAsync(storeId);
            if (res?.Success == true && res.Data != null)
            {
                var shift = res.Data;
                var localTime = shift.OpenedAt.ToLocalTime();
                ShiftName = $"Ca mở lúc {localTime:HH:mm} (Tiền đầu ca: {shift.OpeningCash:N0}đ)";
            }
            else
            {
                ShiftName = "Chưa mở ca làm việc";
            }
        }
        catch
        {
            ShiftName = "Chưa mở ca";
        }
    }

    [RelayCommand]
    private void OpenManagement()
    {
        // Chỉ cho phép nếu user là StoreManager hoặc Owner
        if (_sessionService.IsManager)
        {
            _clockTimer.Stop();
            _navigationService.NavigateTo<ManagementMainView>();
        }
    }

    [RelayCommand]
    private void Refresh()
    {
        CurrentTime = DateTime.Now.ToString("HH:mm:ss");
        _ = LoadCurrentShiftAsync();
    }

    [RelayCommand]
    private void ToggleLanguage()
    {
        CurrentLanguage = CurrentLanguage == "VI" ? "EN" : "VI";
    }


    [RelayCommand]
    private async Task LogoutAsync()
    {
        _clockTimer.Stop();
        var refreshToken = _sessionService.RefreshToken;
        if (!string.IsNullOrWhiteSpace(refreshToken))
        {
            try
            {
                await _authApiClient.LogoutAsync(refreshToken);
            }
            catch
            {
                // Bỏ qua lỗi mạng khi logout để người dùng luôn có thể đăng xuất cục bộ
            }
        }
        _sessionService.Clear();
        _navigationService.NavigateTo<LoginView>();
    }
}
