using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using POS.Contracts.V1.Categories;
using POS.Contracts.V1.Common;
using POS.Contracts.V1.Products;
using POS.WinUI.Core.ApiClients;
using POS.WinUI.Core.Services;
using POS.WinUI.ViewModels.Common;
using Wpf.Ui.Controls;

namespace POS.WinUI.ViewModels.Products;

public sealed partial class ProductsViewModel : ManagementTabViewModelBase
{
    private readonly ProductApiClient _productApi;
    private readonly CategoryApiClient _categoryApi;
    private readonly IConfirmDialogService _confirmDialog;
    private readonly IQueryCacheService _queryCache;

    public override string TabId => "Products";
    public override string Title => "Sản phẩm & Danh mục";
    public override string Subtitle => "Quản lý danh mục hàng hóa, tạo biến thể màu/size, mã vạch SKU và in tem nhãn";
    public override SymbolRegular Icon => SymbolRegular.Box24;

    // ── Phân tab chính: 0 = Hàng hóa / Sản phẩm, 1 = Cây danh mục ──
    [ObservableProperty]
    private int _selectedViewMode = 0; // 0: Sản phẩm, 1: Danh mục

    public bool IsProductViewMode
    {
        get => SelectedViewMode == 0;
        set
        {
            if (value) SelectedViewMode = 0;
        }
    }

    public bool IsCategoryViewMode
    {
        get => SelectedViewMode == 1;
        set
        {
            if (value) SelectedViewMode = 1;
        }
    }

    partial void OnSelectedViewModeChanged(int value)
    {
        OnPropertyChanged(nameof(IsProductViewMode));
        OnPropertyChanged(nameof(IsCategoryViewMode));
    }

    // ── Danh sách dữ liệu ──
    public ObservableCollection<ProductItemModel> Products { get; } = new();
    public ObservableCollection<CategoryItemModel> CategoriesTree { get; } = new();
    public ObservableCollection<CategoryItemModel> CategoryFilterOptions { get; } = new();
    public ObservableCollection<string> StatusFilterOptions { get; } = new() { "Tất cả trạng thái", "Đang bán (Active)", "Ngưng bán (Inactive)" };
    public ObservableCollection<string> BaseUnitOptions { get; } = new() { "Cái", "Hộp", "Chai", "Gói", "Lon", "Bộ", "Kg", "Ly", "Bao", "Thùng" };

    // ── Bộ lọc & Tìm kiếm ──
    [ObservableProperty]
    private string _searchKeyword = string.Empty;

    [ObservableProperty]
    private CategoryItemModel? _selectedCategoryFilter;

    [ObservableProperty]
    private string _selectedStatusFilter = "Tất cả trạng thái";

    private bool _isSuppressFilterCallback = false;

    partial void OnSelectedCategoryFilterChanged(CategoryItemModel? value)
    {
        if (_isSuppressFilterCallback) return;
        CurrentPage = 1;
        _ = LoadProductsAsync();
    }

    partial void OnSelectedStatusFilterChanged(string value)
    {
        if (_isSuppressFilterCallback) return;
        CurrentPage = 1;
        _ = LoadProductsAsync();
    }

    // ── Phân trang ──
    [ObservableProperty]
    private int _currentPage = 1;

    [ObservableProperty]
    private int _pageSize = 15;

    [ObservableProperty]
    private int _totalPages = 1;

    [ObservableProperty]
    private int _totalCount = 0;

    public string PaginationSummary => TotalCount == 0 
        ? "0 / 0 sản phẩm" 
        : $"Trang {CurrentPage} / {Math.Max(1, TotalPages)} (Tổng cộng {TotalCount} sản phẩm)";

    public bool HasPreviousPage => CurrentPage > 1;
    public bool HasNextPage => CurrentPage < TotalPages;
    public bool HasNoProducts => Products.Count == 0 && !IsLoading;
    public bool HasProducts => Products.Count > 0;

    // ── Số liệu thống kê ──
    [ObservableProperty]
    private int _totalProductsCount = 0;

    [ObservableProperty]
    private int _activeProductsCount = 0;

    [ObservableProperty]
    private int _totalSkusCount = 0;

    [ObservableProperty]
    private int _totalCategoriesCount = 0;

    // ── Trạng thái & Lỗi ──
    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private bool _isStatusError = false;

    [ObservableProperty]
    private ProductItemModel? _selectedProduct;

    public ProductsViewModel(
        ProductApiClient productApi,
        CategoryApiClient categoryApi,
        SessionService sessionService,
        IConfirmDialogService confirmDialog,
        IQueryCacheService queryCache) : base(sessionService)
    {
        _productApi = productApi;
        _categoryApi = categoryApi;
        _confirmDialog = confirmDialog;
        _queryCache = queryCache;

        // Initialize helper & barcode
        InitializeBarcode();

        // Tự động tải dữ liệu ban đầu
        _ = LoadInitialDataAsync();
    }

    private async Task LoadInitialDataAsync()
    {
        _isSuppressFilterCallback = true;
        try
        {
            await LoadCategoriesAsync();
        }
        finally
        {
            _isSuppressFilterCallback = false;
        }

        await LoadProductsAsync();
        await LoadKpiSummaryAsync();
    }

    /// <summary>
    /// Nạp số liệu thống kê KPI chuẩn toàn hệ thống từ Backend (Tổng sản phẩm, Đang kinh doanh, SKU, Danh mục)
    /// </summary>
    [RelayCommand]
    public async Task LoadKpiSummaryAsync()
    {
        try
        {
            var cacheKey = $"kpi-summary-{CurrentStoreId}";
            var kpiRes = await _queryCache.GetOrFetchAsync(
                cacheKey,
                () => _productApi.GetProductKpiSummaryAsync(),
                staleTime: TimeSpan.FromSeconds(30));

            if (kpiRes?.Data != null)
            {
                TotalProductsCount = kpiRes.Data.TotalProducts;
                ActiveProductsCount = kpiRes.Data.ActiveProducts;
                TotalSkusCount = kpiRes.Data.TotalSkus;
                TotalCategoriesCount = kpiRes.Data.TotalCategories;
            }
        }
        catch
        {
            // Giữ nguyên số liệu fallback nếu API gặp sự cố
        }
    }

    /// <summary>
    /// Tải cây danh mục từ backend API
    /// </summary>
    [RelayCommand]
    public async Task LoadCategoriesAsync(CancellationToken ct = default)
    {
        try
        {
            var res = await _categoryApi.GetCategoriesTreeAsync(ct);
            if (res?.Data != null)
            {
                _isSuppressFilterCallback = true;
                var currentSelectedId = SelectedCategoryFilter?.Id;

                CategoriesTree.Clear();
                CategoryFilterOptions.Clear();

                // Mục "Tất cả danh mục" đầu tiên cho bộ lọc
                var allItem = new CategoryItemModel { Id = Guid.Empty, Name = "Tất cả danh mục", Level = 0 };
                CategoryFilterOptions.Add(allItem);

                int catCount = 0;
                void AddCategoryRecursive(CategoryResponse dto, int level, ObservableCollection<CategoryItemModel> targetCollection)
                {
                    catCount++;
                    var item = new CategoryItemModel
                    {
                        Id = dto.Id,
                        ParentId = dto.ParentId,
                        Name = dto.Name,
                        DisplayOrder = dto.DisplayOrder,
                        ImageUrl = dto.ImageUrl,
                        IsVisible = dto.IsVisible,
                        Level = level
                    };
                    targetCollection.Add(item);
                    CategoryFilterOptions.Add(item);

                    if (dto.SubCategories != null)
                    {
                        foreach (var sub in dto.SubCategories)
                        {
                            AddCategoryRecursive(sub, level + 1, item.SubCategories);
                        }
                    }
                }

                foreach (var cat in res.Data)
                {
                    AddCategoryRecursive(cat, 0, CategoriesTree);
                }

                TotalCategoriesCount = catCount;
                if (currentSelectedId.HasValue && currentSelectedId.Value != Guid.Empty)
                {
                    SelectedCategoryFilter = CategoryFilterOptions.FirstOrDefault(c => c.Id == currentSelectedId.Value) ?? allItem;
                }
                else
                {
                    SelectedCategoryFilter = allItem;
                }

                _isSuppressFilterCallback = false;
            }
        }
        catch (Exception ex)
        {
            _isSuppressFilterCallback = false;
            ShowStatus($"Lỗi tải danh mục: {ex.Message}", isError: true);
        }
    }

    /// <summary>
    /// Tải danh sách sản phẩm phân trang kèm bộ lọc
    /// </summary>
    [RelayCommand]
    public async Task LoadProductsAsync(CancellationToken ct = default)
    {
        if (IsLoading) return;
        IsLoading = true;
        ShowStatus("Đang tải dữ liệu sản phẩm...");

        try
        {
            Guid? catId = (SelectedCategoryFilter != null && SelectedCategoryFilter.Id != Guid.Empty) 
                ? SelectedCategoryFilter.Id 
                : null;

            string? status = null;
            if (SelectedStatusFilter.Contains("Active")) status = "Active";
            else if (SelectedStatusFilter.Contains("Inactive")) status = "Inactive";

            var search = string.IsNullOrWhiteSpace(SearchKeyword) ? null : SearchKeyword.Trim();

            // Áp dụng Stale-While-Revalidate Query Cache (TanStack Query style)
            var queryKey = $"products-{CurrentStoreId}-{catId}-{status}-{search}-{CurrentPage}-{PageSize}";

            var response = await _queryCache.GetOrFetchAsync(
                queryKey,
                () => _productApi.GetProductsAsync(
                    categoryId: catId,
                    search: search,
                    status: status,
                    pageNumber: CurrentPage,
                    pageSize: PageSize,
                    ct: ct),
                staleTime: TimeSpan.FromSeconds(30));

            Products.Clear();

            if (response?.Data != null)
            {
                var paged = response.Data;
                TotalCount = paged.TotalCount;
                TotalPages = (int)Math.Ceiling((double)TotalCount / PageSize);
                if (TotalPages < 1) TotalPages = 1;

                foreach (var item in paged.Items)
                {
                    var model = new ProductItemModel
                    {
                        Id = item.Id,
                        CategoryId = item.CategoryId,
                        CategoryName = item.CategoryName,
                        Name = item.Name,
                        Brand = item.Brand,
                        BaseUnit = item.BaseUnit,
                        ImageUrl = item.ImageUrl,
                        Status = item.Status,
                        SkuCount = item.SkuCount,
                        CreatedAt = item.CreatedAt
                    };

                    Products.Add(model);
                }

                // Nếu chưa nạp KPI toàn cục từ endpoint summary thì mới fallback tạm
                if (TotalProductsCount == 0)
                {
                    TotalProductsCount = TotalCount;
                    ActiveProductsCount = TotalCount;
                }

                OnPropertyChanged(nameof(PaginationSummary));
                OnPropertyChanged(nameof(HasPreviousPage));
                OnPropertyChanged(nameof(HasNextPage));
                ShowStatus($"Đã tải {Products.Count} sản phẩm thành công.");
            }
            else
            {
                TotalCount = 0;
                TotalPages = 1;
                ShowStatus("Không có sản phẩm nào phù hợp.", isError: false);
            }
        }
        catch (Exception ex)
        {
            ShowStatus($"Lỗi khi tải sản phẩm: {ex.Message}", isError: true);
        }
        finally
        {
            IsLoading = false;
            OnPropertyChanged(nameof(HasNoProducts));
            OnPropertyChanged(nameof(HasProducts));
        }
    }

    [RelayCommand]
    public async Task SearchAsync()
    {
        CurrentPage = 1;
        await LoadProductsAsync();
    }

    [RelayCommand]
    public async Task ClearSearchAsync()
    {
        _isSuppressFilterCallback = true;
        SearchKeyword = string.Empty;
        SelectedCategoryFilter = CategoryFilterOptions.FirstOrDefault();
        SelectedStatusFilter = StatusFilterOptions[0];
        _isSuppressFilterCallback = false;
        CurrentPage = 1;
        await LoadProductsAsync();
    }

    [RelayCommand]
    public async Task NextPageAsync()
    {
        if (CurrentPage < TotalPages)
        {
            CurrentPage++;
            await LoadProductsAsync();
        }
    }

    [RelayCommand]
    public async Task PreviousPageAsync()
    {
        if (CurrentPage > 1)
        {
            CurrentPage--;
            await LoadProductsAsync();
        }
    }

    /// <summary>
    /// Mở rộng / Thu gọn xem danh sách biến thể SKU trực tiếp dưới sản phẩm
    /// </summary>
    [RelayCommand]
    public async Task ToggleProductExpandAsync(ProductItemModel? product)
    {
        if (product == null) return;

        if (product.IsExpanded)
        {
            product.IsExpanded = false;
            return;
        }

        product.IsExpanded = true;

        if (product.Skus.Count == 0)
        {
            product.IsLoadingSkus = true;
            try
            {
                var detailRes = await _productApi.GetProductByIdAsync(product.Id);
                if (detailRes?.Data?.Skus != null)
                {
                    product.Skus.Clear();
                    foreach (var sku in detailRes.Data.Skus)
                    {
                        var (color, size) = ProductSkuItemModel.ParseAttributes(sku.Attributes);
                        product.Skus.Add(new ProductSkuItemModel
                        {
                            Id = sku.Id,
                            ProductId = sku.ProductId,
                            SkuCode = sku.SkuCode,
                            Barcode = sku.Barcode,
                            CostPrice = sku.CostPrice,
                            SellPrice = sku.SellPrice,
                            TaxRate = sku.TaxRate,
                            IsActive = sku.IsActive,
                            Color = color,
                            Size = size
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                ShowStatus($"Lỗi tải SKU của {product.Name}: {ex.Message}", isError: true);
            }
            finally
            {
                product.IsLoadingSkus = false;
            }
        }
    }

    /// <summary>
    /// Bật/tắt trạng thái Active / Inactive của sản phẩm
    /// </summary>
    [RelayCommand]
    public async Task ToggleProductStatusAsync(ProductItemModel? product)
    {
        if (product == null) return;

        var newStatus = product.IsActive ? "Inactive" : "Active";
        try
        {
            var updateReq = new UpdateProductRequest(
                Name: product.Name,
                CategoryId: product.CategoryId,
                BaseUnit: product.BaseUnit,
                Brand: product.Brand,
                ImageUrl: product.ImageUrl,
                Status: newStatus
            );

            var res = await _productApi.UpdateProductAsync(product.Id, updateReq);
            if (res != null && res.Success)
            {
                product.Status = newStatus;
                ShowStatus($"Đã chuyển sản phẩm '{product.Name}' sang trạng thái {(product.IsActive ? "Đang bán" : "Ngưng bán")}.");
            }
            else
            {
                ShowStatus(res?.Error?.Message ?? "Không thể cập nhật trạng thái.", isError: true);
            }
        }
        catch (Exception ex)
        {
            ShowStatus($"Lỗi: {ex.Message}", isError: true);
        }
    }

    public void ShowStatus(string message, bool isError = false)
    {
        if (string.IsNullOrWhiteSpace(message)) return;
        StatusMessage = message;
        IsStatusError = isError;

        if (isError)
        {
            ShowError(message);
        }
        else
        {
            ShowSuccess(message);
        }
    }
}
