using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using POS.WinUI.Core.ApiClients;
using POS.WinUI.Core.Models;
using POS.WinUI.Core.Services;
using POS.WinUI.ViewModels.Common;
using Wpf.Ui.Controls;

namespace POS.WinUI.ViewModels.Shell;

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
    private StoreItem? _selectedStore;

    public ObservableCollection<StoreItem> Stores { get; } = new();

    [ObservableProperty]
    private string _shiftName = "Đang kiểm tra ca...";

    [ObservableProperty]
    private string _registerName = "Quầy 01";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasOpenShift))]
    private Guid? _currentShiftId;

    public bool HasOpenShift => CurrentShiftId.HasValue;

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
}
