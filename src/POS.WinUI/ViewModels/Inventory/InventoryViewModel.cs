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
    public override string Title => "Quản lý Kho & Nhập hàng";
    public override string Subtitle => "Kiểm soát số lượng tồn kho, tạo phiếu nhập hàng NCC và kiểm kê điều chỉnh tồn";
    public override SymbolRegular Icon => SymbolRegular.Archive24;

    // ── Phân tab: 0 = Nhập kho (mặc định active), 1 = Kiểm kê, 2 = Cảnh báo tồn kho ──
    [ObservableProperty]
    private int _selectedTabIndex = 0;

    public bool IsStockInTab => SelectedTabIndex == 0;
    public bool IsStockTakeTab => SelectedTabIndex == 1;
    public bool IsStockTab => SelectedTabIndex == 2;

    partial void OnSelectedTabIndexChanged(int value)
    {
        OnPropertyChanged(nameof(IsStockInTab));
        OnPropertyChanged(nameof(IsStockTakeTab));
        OnPropertyChanged(nameof(IsStockTab));

        if (value == 0)
        {
            _ = LoadStockInVouchersAsync();
            _ = LoadSuppliersAsync();
        }
        else if (value == 1)
        {
            _ = LoadStockTakesAsync();
        }
        else if (value == 2)
        {
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
}
