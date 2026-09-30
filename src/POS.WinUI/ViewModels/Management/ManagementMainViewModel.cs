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
    private readonly DispatcherTimer _clockTimer;

    private readonly Dictionary<string, object> _tabViewModelCache = new();

    [ObservableProperty]
    private object? _currentTabViewModel;

    [ObservableProperty]
    private string _storeName = "Chi nhánh Chưa chọn";

    [ObservableProperty]
    private string _shiftName = "Đang kiểm tra ca...";

    [ObservableProperty]
    private string _employeeName = "Nhân viên";

    [ObservableProperty]
    private string _roleName = "Quản lý cửa hàng";

    [ObservableProperty]
    private string _avatarInitials = "NA";

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
        ShiftApiClient shiftApiClient)
    {
        _serviceProvider = serviceProvider;
        _sessionService = sessionService;
        _networkStatusService = networkStatusService;
        _navigationService = navigationService;
        _shiftApiClient = shiftApiClient;

        LoadUserInfo();
        InitializeNavigationTabs();
        _ = LoadCurrentShiftAsync();

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

        if (!string.IsNullOrWhiteSpace(_sessionService.StoreName))
        {
            StoreName = _sessionService.StoreName;
        }
        else if (!string.IsNullOrWhiteSpace(_sessionService.StoreId))
        {
            StoreName = "Chi nhánh mặc định";
        }
    }

    private static string GetInitials(string fullName)
    {
        var parts = fullName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return "POS";
        if (parts.Length == 1) return parts[0][..Math.Min(2, parts[0].Length)].ToUpperInvariant();
        return $"{char.ToUpperInvariant(parts[0][0])}{char.ToUpperInvariant(parts[^1][0])}";
    }

    private void InitializeNavigationTabs()
    {
        NavTabs.Clear();

        var tabs = new List<NavigationItemViewModel>
        {
            new() { Id = "Dashboard", Title = "Tổng quan", Icon = SymbolRegular.Grid24 },
            new() { Id = "Orders", Title = "Đơn hàng & Hóa đơn", Icon = SymbolRegular.Receipt24 },
            new() { Id = "Products", Title = "Sản phẩm & Danh mục", Icon = SymbolRegular.Box24 },
            new() { Id = "Inventory", Title = "Quản lý Kho & Nhập hàng", Icon = SymbolRegular.Archive24 },
            new() { Id = "Customers", Title = "Khách hàng & Hội viên", Icon = SymbolRegular.People24 },
            new() { Id = "Employees", Title = "Nhân viên & Phân quyền", Icon = SymbolRegular.PersonAccounts24 },
            new() { Id = "Reports", Title = "Báo cáo & Thống kê", Icon = SymbolRegular.DataPie24 },
            new() { Id = "Settings", Title = "Cài đặt hệ thống", Icon = SymbolRegular.Settings24 }
        };

        foreach (var tab in tabs)
        {
            NavTabs.Add(tab);
        }

        // Mặc định chọn tab Tổng quan
        SelectTab(NavTabs[0]);
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
        // TODO: Chuyển hướng sang màn hình bán hàng PosCashierMainView khi đã triển khai
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
    private void Logout()
    {
        _clockTimer.Stop();
        _sessionService.Clear();
        _navigationService.NavigateTo<LoginView>();
    }
}
