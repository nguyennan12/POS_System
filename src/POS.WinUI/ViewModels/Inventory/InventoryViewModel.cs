using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using POS.Contracts.V1.Categories;
using POS.WinUI.Core.ApiClients;
using POS.WinUI.Core.Services;
using POS.WinUI.ViewModels.Common;
using POS.WinUI.ViewModels.Products;
using Wpf.Ui.Controls;

namespace POS.WinUI.ViewModels.Inventory;

/// <summary>
/// ViewModel chính cho phân hệ Quản lý Kho, Nhập hàng và Kiểm kê (T33)
/// </summary>
public sealed partial class InventoryViewModel : ManagementTabViewModelBase
{
    private readonly InventoryApiClient _inventoryApi;
    private readonly ProductApiClient _productApi;
    private readonly CategoryApiClient _categoryApi;

    public override string TabId => "Inventory";
    public override string Title => Loc["inventory.title"];
    public override string Subtitle => Loc["inventory.subtitle"];
    public override SymbolRegular Icon => SymbolRegular.Archive24;

    // ── Quản lý ẩn/hiện Thẻ Thống Kê ──
    [ObservableProperty]
    private bool _isStatsVisible = true;

    [RelayCommand]
    public void ToggleStats() => IsStatsVisible = !IsStatsVisible;

    // ── Phân tab: 0 = Tra cứu Tồn kho, 1 = Phiếu Nhập kho, 2 = Phiếu Kiểm kê, 3 = Cảnh báo thiếu hàng ──
    [ObservableProperty]
    private int _selectedTabIndex = 0;

    public bool IsStockTab
    {
        get => SelectedTabIndex == 0;
        set { if (value) SelectedTabIndex = 0; }
    }

    public bool IsStockInTab
    {
        get => SelectedTabIndex == 1;
        set { if (value) SelectedTabIndex = 1; }
    }

    public bool IsStockTakeTab
    {
        get => SelectedTabIndex == 2;
        set { if (value) SelectedTabIndex = 2; }
    }

    public bool IsStockAlertTab
    {
        get => SelectedTabIndex == 3;
        set { if (value) SelectedTabIndex = 3; }
    }

    partial void OnSelectedTabIndexChanged(int value)
    {
        OnPropertyChanged(nameof(IsStockTab));
        OnPropertyChanged(nameof(IsStockInTab));
        OnPropertyChanged(nameof(IsStockTakeTab));
        OnPropertyChanged(nameof(IsStockAlertTab));

        if (value == 0)
        {
            ShowOnlyAlerts = false;
            _ = LoadStockEntriesAsync();
        }
        else if (value == 1)
        {
            _ = LoadStockInVouchersAsync();
            _ = LoadSuppliersAsync();
        }
        else if (value == 2)
        {
            _ = LoadStockTakesAsync();
        }
        else if (value == 3)
        {
            ShowOnlyAlerts = true;
            _ = LoadStockEntriesAsync();
            _ = LoadStockAlertsAsync();
        }
    }

    // ── Thông báo Toast / Feedback ──
    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private bool _isStatusError = false;

    public void ShowNotification(string message, bool isError = false)
    {
        StatusMessage = message;
        IsStatusError = isError;

        // Auto clear sau 5 giây
        Task.Delay(5000).ContinueWith(_ =>
        {
            if (StatusMessage == message)
            {
                StatusMessage = string.Empty;
            }
        });
    }

    [RelayCommand]
    public void ClearNotification()
    {
        StatusMessage = string.Empty;
    }

    // ── Constructor ──
    public InventoryViewModel(
        InventoryApiClient inventoryApi,
        ProductApiClient productApi,
        CategoryApiClient categoryApi,
        SessionService sessionService) : base(sessionService)
    {
        _inventoryApi = inventoryApi;
        _productApi = productApi;
        _categoryApi = categoryApi;

        // Tự động tải danh mục và tồn kho
        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        await LoadSuppliersAsync();
        await LoadStockInVouchersAsync();
        await LoadStockTakesAsync();
        await LoadCategoriesFilterAsync();
        await LoadStockEntriesAsync();
        await LoadStockAlertsAsync();
    }

    [RelayCommand]
    public async Task RefreshAllAsync()
    {
        if (SelectedTabIndex == 0)
        {
            await LoadStockInVouchersAsync();
        }
        else if (SelectedTabIndex == 1)
        {
            await LoadStockTakesAsync();
        }
        else if (SelectedTabIndex == 2)
        {
            await LoadStockEntriesAsync();
            await LoadStockAlertsAsync();
        }
    }

    protected override void OnStoreChanged(string storeId, string? storeName)
    {
        _ = InitializeAsync();
    }
}
