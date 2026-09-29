using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using POS.Contracts.V1.Products;
using POS.WinUI.ApiClients;
using POS.WinUI.Models;
using POS.WinUI.Services;

namespace POS.WinUI.ViewModels.Products;

public partial class ProductListViewModel : ObservableObject
{
    private readonly ProductApiClient _productApi;
    private readonly CategoryApiClient _categoryApi;

    // ── Data ──────────────────────────────────────────────────────────────────
    [ObservableProperty] private ObservableCollection<ProductListItem> _products = new();
    [ObservableProperty] private ObservableCollection<CategoryItem> _categories = new();
    [ObservableProperty] private CategoryItem? _selectedCategory;

    // ── Filters ───────────────────────────────────────────────────────────────
    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private string _selectedStatus = string.Empty; // "" = all

    // ── Paging ────────────────────────────────────────────────────────────────
    [ObservableProperty] private int _currentPage = 1;
    [ObservableProperty] private int _totalPages  = 1;
    [ObservableProperty] private int _totalCount;
    private const int PageSize = 20;

    // ── State ─────────────────────────────────────────────────────────────────
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string? _errorMessage;
    [ObservableProperty] private ProductListItem? _selectedProduct;

    public bool CanGoPrev => CurrentPage > 1;
    public bool CanGoNext => CurrentPage < TotalPages;

    public ProductListViewModel(
        ProductApiClient productApi,
        CategoryApiClient categoryApi)
    {
        _productApi = productApi;
        _categoryApi = categoryApi;
    }

    // ── Init ──────────────────────────────────────────────────────────────────

    [RelayCommand]
    public async Task InitializeAsync()
    {
        await LoadCategoriesAsync();
        await LoadProductsAsync();
    }

    private async Task LoadCategoriesAsync()
    {
        try
        {
            var res = await _categoryApi.GetAllAsync();
            if (res?.Success == true && res.Data != null)
            {
                Categories.Clear();
                Categories.Add(new CategoryItem { Id = null, Name = "Tất cả danh mục" });
                foreach (var c in res.Data)
                    Categories.Add(new CategoryItem { Id = c.Id, Name = c.Name });
                SelectedCategory = Categories[0];
            }
        }
        catch { /* silent — user sees empty filter */ }
    }

    // ── Search & Filter ───────────────────────────────────────────────────────

    /// <summary>Gọi khi user nhập text tìm kiếm (debounce thực hiện ở View).</summary>
    [RelayCommand]
    public async Task SearchAsync()
    {
        CurrentPage = 1;
        await LoadProductsAsync();
    }

    partial void OnSelectedCategoryChanged(CategoryItem? value) =>
        _ = SearchAsync();

    partial void OnSelectedStatusChanged(string value) =>
        _ = SearchAsync();

    // ── Load ──────────────────────────────────────────────────────────────────

    private async Task LoadProductsAsync()
    {
        IsLoading = true;
        ErrorMessage = null;
        try
        {
            var filter = new ProductFilterRequest(
                Search:     string.IsNullOrWhiteSpace(SearchText) ? null : SearchText.Trim(),
                CategoryId: SelectedCategory?.Id,
                Status:     string.IsNullOrWhiteSpace(SelectedStatus) ? null : SelectedStatus,
                PageNumber: CurrentPage,
                PageSize:   PageSize);

            var res = await _productApi.GetProductsAsync(filter);
            if (res?.Success == true && res.Data != null)
            {
                Products.Clear();
                foreach (var p in res.Data.Items)
                    Products.Add(new ProductListItem
                    {
                        Id           = p.Id,
                        Name         = p.Name,
                        CategoryName = p.CategoryName,
                        Brand        = p.Brand,
                        BaseUnit     = p.BaseUnit,
                        Status       = p.Status,
                        SkuCount     = p.SkuCount,
                    });

                TotalCount = res.Data.TotalCount;
                TotalPages = res.Data.TotalPages > 0 ? res.Data.TotalPages : 1;
            }
            else
            {
                ErrorMessage = res?.Error?.Message ?? "Không tải được danh sách sản phẩm.";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsLoading = false;
            OnPropertyChanged(nameof(CanGoPrev));
            OnPropertyChanged(nameof(CanGoNext));
        }
    }

    // ── Pagination ────────────────────────────────────────────────────────────

    [RelayCommand(CanExecute = nameof(CanGoPrev))]
    private async Task PrevPageAsync()
    {
        CurrentPage--;
        await LoadProductsAsync();
    }

    [RelayCommand(CanExecute = nameof(CanGoNext))]
    private async Task NextPageAsync()
    {
        CurrentPage++;
        await LoadProductsAsync();
    }

    // ── Navigation ────────────────────────────────────────────────────────────

    [RelayCommand]
    public async Task OpenCreateAsync()
    {
        var editVm = App.Services.GetService(typeof(ProductEditViewModel)) as ProductEditViewModel
                     ?? throw new InvalidOperationException();
        await editVm.InitializeForCreateAsync();
        OpenEditWindow(editVm);
    }

    [RelayCommand]
    public async Task OpenEditAsync(ProductListItem? item)
    {
        if (item is null) return;
        var editVm = App.Services.GetService(typeof(ProductEditViewModel)) as ProductEditViewModel
                     ?? throw new InvalidOperationException();
        await editVm.InitializeForEditAsync(item.Id);
        OpenEditWindow(editVm);
    }

    private void OpenEditWindow(ProductEditViewModel vm)
    {
        var win = new Views.Products.ProductEditView { DataContext = vm };
        vm.CloseRequested += (saved) =>
        {
            win.Close();
            if (saved) _ = LoadProductsAsync();
        };
        win.ShowDialog();
    }
}
