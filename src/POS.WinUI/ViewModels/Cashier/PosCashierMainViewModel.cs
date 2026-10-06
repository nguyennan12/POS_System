using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using POS.WinUI.Core.ApiClients;
using POS.WinUI.Core.Services;
using POS.WinUI.Views.Auth;
using POS.WinUI.Views.Shell;

namespace POS.WinUI.ViewModels.Cashier;

public partial class PosCashierMainViewModel : ObservableObject
{
    private readonly SessionService _sessionService;
    private readonly NetworkStatusService _networkStatusService;
    private readonly INavigationService _navigationService;
    private readonly ICfdDisplayService _cfdDisplayService;
    private readonly ICfdSyncService _cfdSyncService;
    private readonly ShiftApiClient _shiftApiClient;
    private readonly StoreApiClient _storeApiClient;
    private readonly AuthApiClient _authApiClient;
    private readonly CategoryApiClient _categoryApiClient;
    private readonly InventoryApiClient _inventoryApiClient;
    private readonly CustomerApiClient _customerApiClient;
    private readonly VoucherApiClient _voucherApiClient;
    private readonly OrderApiClient _orderApiClient;
    private readonly ProductApiClient _productApiClient;

    [RelayCommand]
    private void ToggleCfd()
    {
        _cfdDisplayService.ToggleCfd();
        ShowInfo(_cfdDisplayService.IsOpen ? "Đã bật Màn hình phụ cho khách (CFD)" : "Đã đóng Màn hình phụ cho khách (CFD)");
    }

    private readonly DispatcherTimer _clockTimer;
    private CancellationTokenSource? _searchCts;

    // ════════════════════ THÔNG TIN HEADER & TRẠNG THÁI ════════════════════

    [ObservableProperty]
    private string _storeName = "Cửa hàng OraPOS";

    [ObservableProperty]
    private string _employeeName = "Nhân viên thu ngân";

    [ObservableProperty]
    private string _roleName = "Thu ngân";

    [ObservableProperty]
    private string _avatarInitials = "NV";

    [ObservableProperty]
    private string _shiftName = "Đang kiểm tra ca làm việc...";

    [ObservableProperty]
    private Guid? _currentShiftId;

    [ObservableProperty]
    private string _currentLanguage = "VI";

    [ObservableProperty]
    private bool _isOwnerRole = false;

    [ObservableProperty]
    private bool _isLoadingStores = false;

    public ObservableCollection<POS.Contracts.V1.Stores.StoreResponse> Stores { get; } = new();

    [ObservableProperty]
    private POS.Contracts.V1.Stores.StoreResponse? _selectedStore;

    [ObservableProperty]
    private bool _isConnected = true;

    [ObservableProperty]
    private string _statusText = "Sẵn sàng";

    [ObservableProperty]
    private string _currentTime = DateTime.Now.ToString("HH:mm:ss");

    [ObservableProperty]
    private string _appVersion = "OraPOS v1.0.0";

    [ObservableProperty]
    private bool _isManagerRole = false;

    // ════════════════════ TOAST NOTIFICATION ════════════════════
    [ObservableProperty]
    private string? _toastMessage;

    [ObservableProperty]
    private string _toastType = "Info";

    public void ShowToast(string message, string type = "Info")
    {
        ToastType = type;
        if (ToastMessage == message)
        {
            ToastMessage = string.Empty;
        }
        ToastMessage = message;
    }

    public void ShowError(string message) => ShowToast(message, "Error");
    public void ShowSuccess(string message) => ShowToast(message, "Success");
    public void ShowWarning(string message) => ShowToast(message, "Warning");
    public void ShowInfo(string message) => ShowToast(message, "Info");

    public PosCashierMainViewModel(
        SessionService sessionService,
        NetworkStatusService networkStatusService,
        INavigationService navigationService,
        ICfdDisplayService cfdDisplayService,
        ICfdSyncService cfdSyncService,
        ShiftApiClient shiftApiClient,
        StoreApiClient storeApiClient,
        AuthApiClient authApiClient,
        CategoryApiClient categoryApiClient,
        InventoryApiClient inventoryApiClient,
        CustomerApiClient customerApiClient,
        VoucherApiClient voucherApiClient,
        OrderApiClient orderApiClient,
        ProductApiClient productApiClient)
    {
        _sessionService = sessionService;
        _networkStatusService = networkStatusService;
        _navigationService = navigationService;
        _cfdDisplayService = cfdDisplayService;
        _cfdSyncService = cfdSyncService;
        _shiftApiClient = shiftApiClient;
        _storeApiClient = storeApiClient;
        _authApiClient = authApiClient;
        _categoryApiClient = categoryApiClient;
        _inventoryApiClient = inventoryApiClient;
        _customerApiClient = customerApiClient;
        _voucherApiClient = voucherApiClient;
        _orderApiClient = orderApiClient;
        _productApiClient = productApiClient;

        LoadUserInfo();
        _ = LoadCurrentShiftAsync();
        _ = LoadMemberTiersAsync();

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
                _ = LoadCatalogFromApiAsync();
                _ = LoadMemberTiersAsync();
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

        // Gọi API tải danh mục & sản phẩm thực tế từ backend
        _ = LoadCatalogFromApiAsync();
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
    }

    [RelayCommand]
    public async Task LoadStoresAsync()
    {
        IsLoadingStores = true;
        try
        {
            var res = await _storeApiClient.GetPublicStoresAsync();
            if (res?.Success == true && res.Data != null)
            {
                Stores.Clear();
                foreach (var store in res.Data)
                {
                    Stores.Add(store);
                }

                if (!string.IsNullOrWhiteSpace(_sessionService.StoreId) && Guid.TryParse(_sessionService.StoreId, out var currentStoreId))
                {
                    SelectedStore = Stores.FirstOrDefault(s => s.Id == currentStoreId);
                }
            }
        }
        catch
        {
            // Bỏ qua lỗi tải store
        }
        finally
        {
            IsLoadingStores = false;
        }
    }

    partial void OnSelectedStoreChanged(POS.Contracts.V1.Stores.StoreResponse? value)
    {
        if (value != null && value.Id.ToString() != _sessionService.StoreId)
        {
            _sessionService.SetStore(value.Id.ToString(), value.Name);
            StoreName = value.Name;
            _ = LoadCurrentShiftAsync();
            _ = LoadCatalogFromApiAsync();
        }
    }

    [RelayCommand]
    public async Task LoadCurrentShiftAsync()
    {
        if (string.IsNullOrWhiteSpace(_sessionService.StoreId) || !Guid.TryParse(_sessionService.StoreId, out var storeId))
        {
            ShiftName = "Chưa chọn cửa hàng";
            CurrentShiftId = null;
            return;
        }

        try
        {
            var res = await _shiftApiClient.GetCurrentShiftAsync(storeId);
            if (res?.Success == true && res.Data != null)
            {
                var shift = res.Data;
                CurrentShiftId = shift.ShiftId;
                var localTime = shift.OpenedAt.ToLocalTime();
                ShiftName = $"Ca mở lúc {localTime:HH:mm} (Tiền đầu ca: {shift.OpeningCash:N0}đ)";
            }
            else
            {
                ShiftName = "Chưa mở ca làm việc";
                CurrentShiftId = null;
            }
        }
        catch
        {
            ShiftName = "Chưa mở ca";
            CurrentShiftId = null;
        }
    }

    [RelayCommand]
    private void OpenManagement()
    {
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
        _ = LoadCatalogFromApiAsync();
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
        try
        {
            var refreshToken = _sessionService.RefreshToken;
            if (!string.IsNullOrEmpty(refreshToken))
            {
                await _authApiClient.LogoutAsync(refreshToken);
            }
        }
        catch
        {
            // Bỏ qua lỗi logout API
        }

        _sessionService.Clear();
        _navigationService.NavigateTo<LoginView>();
    }

    private static string GetInitials(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "NV";
        var parts = name.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 1) return parts[0][..Math.Min(2, parts[0].Length)].ToUpperInvariant();
        return $"{parts[0][0]}{parts[^1][0]}".ToUpperInvariant();
    }
}
