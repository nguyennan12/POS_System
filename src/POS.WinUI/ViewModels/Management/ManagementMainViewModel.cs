using System.Collections.ObjectModel;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using POS.WinUI.ApiClients;
using POS.WinUI.Services;
using POS.WinUI.ViewModels.Dashboard;
using POS.WinUI.ViewModels.Management.Tabs;
using POS.WinUI.Views.Auth;
using POS.WinUI.Views.Cashier;
using Wpf.Ui.Controls;

namespace POS.WinUI.ViewModels.Management;

/// <summary>
/// ViewModel chính cho khung layout quản lý (StoreManager / Owner)
/// </summary>
public partial class ManagementMainViewModel : ObservableObject
{
    private readonly IServiceProvider _serviceProvider;
    private readonly SessionService _sessionService;
    private readonly NetworkStatusService _networkStatusService;
    private readonly INavigationService _navigationService;
    private readonly ShiftApiClient _shiftApiClient;
    private readonly StoreApiClient _storeApiClient;
    private readonly AuthApiClient _authApiClient;
    private readonly DispatcherTimer _clockTimer;

    private readonly Dictionary<string, object> _tabViewModelCache = new();

    [ObservableProperty]
    private object? _currentTabViewModel;

    [ObservableProperty]
    private string _storeName = "Chi nhánh Chưa chọn";

    [ObservableProperty]
    private bool _isOwnerRole = false;

    [ObservableProperty]
    private bool _isLoadingStores = false;

    [ObservableProperty]
    private POS.WinUI.Models.StoreItem? _selectedStore;

    public ObservableCollection<POS.WinUI.Models.StoreItem> Stores { get; } = new();

    [ObservableProperty]
    private string _shiftName = "Đang kiểm tra ca...";

    [ObservableProperty]
    private string _employeeName = "Nhân viên";

    [ObservableProperty]
    private string _roleName = "Quản lý cửa hàng";

    [ObservableProperty]
    private string _avatarInitials = "NA";

    [ObservableProperty]
    private string _currentLanguage = "VI";

    [ObservableProperty]
    private int _unreadNotificationCount = 3;

    [ObservableProperty]
    private bool _isConnected = true;

    [ObservableProperty]
    private string _statusText = "Đã kết nối với máy chủ";

    [ObservableProperty]
    private string _currentTime = DateTime.Now.ToString("HH:mm:ss");

    [ObservableProperty]
    private string _appVersion = "OraPOS v1.0.0";

    [ObservableProperty]
    private bool _isManagerRole = true;

    [ObservableProperty]
    private NavigationItemViewModel? _selectedTab;

    [ObservableProperty]
    private string _activeTabTitle = "Tổng quan";

    [ObservableProperty]
    private SymbolRegular _activeTabIcon = SymbolRegular.Grid24;

    [ObservableProperty]
    private string _activeTabBadgeText = "Đang ở: Tổng quan";

    [ObservableProperty]
    private bool _isSidebarExpanded = true;

    [ObservableProperty]
    private bool _isLoadingShift = false;

    public ObservableCollection<NavigationItemViewModel> NavTabs { get; } = new();

    public ManagementMainViewModel(
        IServiceProvider serviceProvider,
        SessionService sessionService,
        NetworkStatusService networkStatusService,
        INavigationService navigationService,
        ShiftApiClient shiftApiClient,
        StoreApiClient storeApiClient,
        AuthApiClient authApiClient)
    {
        _serviceProvider = serviceProvider;
        _sessionService = sessionService;
        _networkStatusService = networkStatusService;
        _navigationService = navigationService;
        _shiftApiClient = shiftApiClient;
        _storeApiClient = storeApiClient;
        _authApiClient = authApiClient;

        LoadUserInfo();
        InitializeNavigationTabs();
        _ = LoadCurrentShiftAsync();

        if (IsOwnerRole)
        {
            _ = LoadStoresAsync();
        }

        // Đồng bộ trạng thái mạng
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
        _clockTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
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
        if (!_sessionService.IsManager && _sessionService.IsCashier)
        {
            _clockTimer.Stop();
            _navigationService.NavigateTo<PosCashierMainView>();
            return;
        }

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
            // Ignore — network handler covers errors
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

        // Xóa cache các tab để nạp lại dữ liệu cho chi nhánh mới
        _tabViewModelCache.Clear();
        if (SelectedTab != null)
        {
            SelectTab(SelectedTab);
        }

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

    private void InitializeNavigationTabs()
    {
        NavTabs.Clear();

        var tabs = new List<NavigationItemViewModel>
        {
            new() { Id = "Dashboard", Title = "Tổng quan", Icon = SymbolRegular.Grid24, MinRoleLevel = 2 },
            new() { Id = "Orders", Title = "Đơn hàng & Hóa đơn", Icon = SymbolRegular.Receipt24, MinRoleLevel = 2 },
            new() { Id = "Products", Title = "Sản phẩm & Danh mục", Icon = SymbolRegular.Box24, MinRoleLevel = 2 },
            new() { Id = "Inventory", Title = "Quản lý Kho & Nhập hàng", Icon = SymbolRegular.Archive24, MinRoleLevel = 2 },
            new() { Id = "Customers", Title = "Khách hàng & Hội viên", Icon = SymbolRegular.People24, MinRoleLevel = 2 },
            new() { Id = "Promotions", Title = "Khuyến mãi & Voucher", Icon = SymbolRegular.TicketDiagonal24, MinRoleLevel = 2 },
            new() { Id = "Employees", Title = "Nhân viên & Phân quyền", Icon = SymbolRegular.PersonAccounts24, MinRoleLevel = 2 },
            new() { Id = "Reports", Title = "Báo cáo & Thống kê", Icon = SymbolRegular.DataPie24, MinRoleLevel = 2 },
            new() { Id = "Settings", Title = "Cài đặt hệ thống", Icon = SymbolRegular.Settings24, MinRoleLevel = 2 }
        };

        foreach (var tab in tabs)
        {
            if (IsTabAllowed(tab))
            {
                NavTabs.Add(tab);
            }
        }

        // Mặc định chọn tab đầu tiên khả dụng
        if (NavTabs.Count > 0)
        {
            SelectTab(NavTabs[0]);
        }
    }

    private bool IsTabAllowed(NavigationItemViewModel tab)
    {
        if (tab.MinRoleLevel > 0 && _sessionService.RoleLevel < tab.MinRoleLevel)
        {
            return false;
        }

        if (tab.AllowedRoles != null && tab.AllowedRoles.Length > 0)
        {
            if (!_sessionService.IsInRole(string.Join(',', tab.AllowedRoles)))
            {
                return false;
            }
        }

        if (tab.RequiredPermissions != null && tab.RequiredPermissions.Length > 0)
        {
            if (!_sessionService.HasAnyPermission(tab.RequiredPermissions))
            {
                return false;
            }
        }

        return true;
    }

    [RelayCommand]
    private void SelectTab(NavigationItemViewModel? tab)
    {
        if (tab == null) return;

        foreach (var item in NavTabs)
        {
            item.IsSelected = (item.Id == tab.Id);
        }

        SelectedTab = tab;
        ActiveTabTitle = tab.Title;
        ActiveTabIcon = tab.Icon;
        ActiveTabBadgeText = $"Đang ở: {tab.Title}";

        // ── Dynamic Sub-ViewModel Switch with Cache ──
        if (!_tabViewModelCache.TryGetValue(tab.Id, out var subVm))
        {
            subVm = tab.Id switch
            {
                "Dashboard" => _serviceProvider.GetRequiredService<DashboardViewModel>(),
                "Orders"    => _serviceProvider.GetRequiredService<OrdersTabViewModel>(),
                "Products"  => _serviceProvider.GetRequiredService<ProductsTabViewModel>(),
                "Inventory" => _serviceProvider.GetRequiredService<InventoryTabViewModel>(),
                "Customers" => _serviceProvider.GetRequiredService<CustomersTabViewModel>(),
                "Promotions"=> _serviceProvider.GetRequiredService<PromotionsTabViewModel>(),
                "Employees" => _serviceProvider.GetRequiredService<EmployeesTabViewModel>(),
                "Reports"   => _serviceProvider.GetRequiredService<ReportsTabViewModel>(),
                "Settings"  => _serviceProvider.GetRequiredService<SettingsTabViewModel>(),
                _ => null
            };

            if (subVm != null)
            {
                _tabViewModelCache[tab.Id] = subVm;
            }
        }

        CurrentTabViewModel = subVm;
    }

    [RelayCommand]
    private void ToggleSidebar()
    {
        IsSidebarExpanded = !IsSidebarExpanded;
    }

    [RelayCommand]
    private void OpenPosCashier()
    {
        _clockTimer.Stop();
        _navigationService.NavigateTo<PosCashierMainView>();
    }

    [RelayCommand]
    public async Task LoadCurrentShiftAsync()
    {
        if (string.IsNullOrWhiteSpace(_sessionService.StoreId) || !Guid.TryParse(_sessionService.StoreId, out var storeId))
        {
            ShiftName = "Chưa mở ca làm việc";
            return;
        }

        IsLoadingShift = true;
        try
        {
            var res = await _shiftApiClient.GetCurrentShiftAsync(storeId);
            if (res?.Success == true && res.Data != null)
            {
                var shift = res.Data;
                _sessionService.SetShift(shift.ShiftId.ToString(), "Ca đang mở");
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
        finally
        {
            IsLoadingShift = false;
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
