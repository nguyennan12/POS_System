using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using POS.Contracts.V1.Categories;
using POS.Contracts.V1.Inventory;
using POS.WinUI.ViewModels.Products;

namespace POS.WinUI.ViewModels.Inventory;

public sealed partial class InventoryViewModel
{
    // ── KPI Thống kê Kho & Cảnh báo (4 Thẻ KPI) ──
    [ObservableProperty]
    private int _totalSkusCount = 0;

    [ObservableProperty]
    private int _lowStockCount = 0;

    [ObservableProperty]
    private int _pendingVouchersCount = 0;

    [ObservableProperty]
    private int _openStockTakesCount = 0;

    [ObservableProperty]
    private int _outOfStockCount = 0;

    [ObservableProperty]
    private int _nearExpiryCount = 0;

    // ── Danh sách dữ liệu Tồn kho & Cảnh báo ──
    public ObservableCollection<StockEntryItemModel> StockEntries { get; } = new();
    public ObservableCollection<StockAlertItemModel> StockAlerts { get; } = new();
    public ObservableCollection<CategoryItemModel> CategoriesFilter { get; } = new();

    // ── Bộ lọc & Tìm kiếm Tồn kho ──
    [ObservableProperty]
    private string _searchKeyword = string.Empty;

    [ObservableProperty]
    private CategoryItemModel? _selectedCategoryFilter;

    [ObservableProperty]
    private bool _showOnlyAlerts = false;

    // ── Phân trang Tồn kho ──
    [ObservableProperty]
    private int _currentPage = 1;

    [ObservableProperty]
    private int _pageSize = 20;

    [ObservableProperty]
    private int _totalPages = 1;

    [ObservableProperty]
    private int _totalCount = 0;

    public string PaginationSummary => TotalCount == 0
        ? "0 / 0 mặt hàng"
        : $"Trang {CurrentPage} / {Math.Max(1, TotalPages)} (Tổng cộng {TotalCount} mặt hàng)";

    public bool HasPreviousPage => CurrentPage > 1;
    public bool HasNextPage => CurrentPage < TotalPages;

    partial void OnSearchKeywordChanged(string value)
    {
        CurrentPage = 1;
        _ = LoadStockEntriesAsync();
    }

    partial void OnSelectedCategoryFilterChanged(CategoryItemModel? value)
    {
        CurrentPage = 1;
        _ = LoadStockEntriesAsync();
    }

    partial void OnShowOnlyAlertsChanged(bool value)
    {
        CurrentPage = 1;
        _ = LoadStockEntriesAsync();
    }

    // ── Tải danh mục lọc ──
    public async Task LoadCategoriesFilterAsync(CancellationToken ct = default)
    {
        try
        {
            var res = await _categoryApi.GetCategoriesTreeAsync(ct);
            if (res?.Data != null)
            {
                CategoriesFilter.Clear();
                CategoriesFilter.Add(new CategoryItemModel { Id = Guid.Empty, Name = "Tất cả danh mục" });

                void AddRecursive(CategoryResponse cat)
                {
                    CategoriesFilter.Add(new CategoryItemModel
                    {
                        Id = cat.Id,
                        Name = cat.Name,
                        ParentId = cat.ParentId
                    });
                    if (cat.SubCategories != null)
                    {
                        foreach (var sub in cat.SubCategories) AddRecursive(sub);
                    }
                }

                foreach (var cat in res.Data) AddRecursive(cat);
                SelectedCategoryFilter = CategoriesFilter.FirstOrDefault();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Inventory] Error loading categories filter: {ex.Message}");
        }
    }

    // ── Tải danh sách tồn kho ──
    [RelayCommand]
    public async Task LoadStockEntriesAsync(CancellationToken ct = default)
    {
        IsLoading = true;
        try
        {
            var catId = SelectedCategoryFilter?.Id == Guid.Empty ? null : SelectedCategoryFilter?.Id;
            var res = await _inventoryApi.GetStockAsync(
                catId,
                string.IsNullOrWhiteSpace(SearchKeyword) ? null : SearchKeyword,
                CurrentPage,
                PageSize,
                ct);

            StockEntries.Clear();
            if (res?.Data != null)
            {
                TotalCount = res.Data.TotalCount;
                TotalPages = Math.Max(1, (int)Math.Ceiling((double)TotalCount / PageSize));

                int outCount = 0;
                int lowCount = 0;

                foreach (var item in res.Data.Items)
                {
                    var model = StockEntryItemModel.FromDto(item);
                    if (model.IsOutOfStock) outCount++;
                    else if (model.IsLowStock) lowCount++;

                    // Nếu người dùng chọn lọc chỉ xem cảnh báo
                    if (ShowOnlyAlerts && !model.IsOutOfStock && !model.IsLowStock)
                        continue;

                    StockEntries.Add(model);
                }

                TotalSkusCount = TotalCount;
                OutOfStockCount = outCount;
                LowStockCount = lowCount;
            }

            OnPropertyChanged(nameof(PaginationSummary));
            OnPropertyChanged(nameof(HasPreviousPage));
            OnPropertyChanged(nameof(HasNextPage));
        }
        catch (Exception ex)
        {
            ShowNotification($"Lỗi tải tồn kho: {ex.Message}", true);
        }
        finally
        {
            IsLoading = false;
        }
    }

    // ── Tải danh sách cảnh báo tồn & hạn dùng ──
    [RelayCommand]
    public async Task LoadStockAlertsAsync(CancellationToken ct = default)
    {
        try
        {
            var res = await _inventoryApi.GetStockAlertsAsync(nearExpiryDays: 30, pageNumber: 1, pageSize: 50, ct);
            StockAlerts.Clear();
            if (res?.Data != null)
            {
                int nearExp = 0;
                foreach (var alert in res.Data.Items)
                {
                    var model = StockAlertItemModel.FromDto(alert);
                    if (model.IsNearExpiry) nearExp++;
                    StockAlerts.Add(model);
                }
                NearExpiryCount = nearExp;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Inventory] Error loading alerts: {ex.Message}");
        }
    }

    [RelayCommand]
    public async Task PreviousPageAsync()
    {
        if (HasPreviousPage)
        {
            CurrentPage--;
            await LoadStockEntriesAsync();
        }
    }

    [RelayCommand]
    public async Task NextPageAsync()
    {
        if (HasNextPage)
        {
            CurrentPage++;
            await LoadStockEntriesAsync();
        }
    }

    // ── Xuất hủy tồn kho (Dispose Stock) ──
    [ObservableProperty]
    private bool _isDisposeModalOpen = false;

    [ObservableProperty]
    private Guid _disposeSkuId;

    [ObservableProperty]
    private string _disposeProductName = string.Empty;

    [ObservableProperty]
    private string _disposeBarcode = string.Empty;

    [ObservableProperty]
    private decimal _disposeCurrentStock = 0;

    [ObservableProperty]
    private decimal _disposeQty = 1;

    [ObservableProperty]
    private string _disposeNote = string.Empty;

    [RelayCommand]
    public void OpenDisposeModal(StockEntryItemModel? item)
    {
        if (item == null) return;
        DisposeSkuId = item.SkuId;
        DisposeProductName = item.ProductName;
        DisposeBarcode = item.Barcode;
        DisposeCurrentStock = item.QtyOnHand;
        DisposeQty = 1;
        DisposeNote = "Hàng hư hỏng / hết hạn sử dụng";
        IsDisposeModalOpen = true;
    }

    [RelayCommand]
    public void CloseDisposeModal()
    {
        IsDisposeModalOpen = false;
    }

    [RelayCommand]
    public async Task ConfirmDisposeStockAsync()
    {
        if (DisposeQty <= 0)
        {
            ShowNotification("Số lượng xuất hủy phải lớn hơn 0.", true);
            return;
        }

        if (DisposeQty > DisposeCurrentStock)
        {
            ShowNotification("Số lượng xuất hủy không được vượt quá số tồn kho hiện tại.", true);
            return;
        }

        IsLoading = true;
        try
        {
            var req = new DisposeStockRequest(DisposeSkuId, DisposeQty, DisposeNote);
            var res = await _inventoryApi.DisposeStockAsync(req);

            if (res != null)
            {
                ShowNotification($"Đã xuất hủy {DisposeQty} {DisposeProductName} thành công.");
                IsDisposeModalOpen = false;
                await LoadStockEntriesAsync();
                await LoadStockAlertsAsync();
            }
        }
        catch (Exception ex)
        {
            ShowNotification($"Lỗi xuất hủy: {ex.Message}", true);
        }
        finally
        {
            IsLoading = false;
        }
    }
}
