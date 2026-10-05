using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using POS.WinUI.Core.Models;
using Wpf.Ui.Controls;

namespace POS.WinUI.ViewModels.Cashier;

public partial class PosCashierMainViewModel
{
    // ════════════════════ DANH MỤC & SẢN PHẨM ════════════════════

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    public ObservableCollection<PosCategoryItem> Categories { get; } = new();

    public ObservableCollection<PosCategoryItem> SubCategories { get; } = new();

    [ObservableProperty]
    private PosCategoryItem? _selectedCategory;

    [ObservableProperty]
    private PosCategoryItem? _selectedSubCategory;

    [ObservableProperty]
    private bool _hasSubCategories = false;

    public ObservableCollection<PosProductItem> FilteredProducts { get; } = new();

    [ObservableProperty]
    private bool _isLoadingCatalog = false;

    [RelayCommand]
    public async Task LoadCatalogFromApiAsync()
    {
        IsLoadingCatalog = true;
        StatusText = "Đang tải dữ liệu...";

        try
        {
            // 1. Tải danh mục từ API
            Categories.Clear();
            SubCategories.Clear();
            HasSubCategories = false;
            SelectedSubCategory = null;

            var allCat = new PosCategoryItem { Id = "ALL", Name = "Tất cả", IsSelected = true, Color = "#EA580C" };
            Categories.Add(allCat);
            SelectedCategory = allCat;

            var catRes = await _categoryApiClient.GetCategoriesTreeAsync();
            if (catRes?.Success == true && catRes.Data != null)
            {
                foreach (var cat in catRes.Data)
                {
                    var rootCatItem = new PosCategoryItem
                    {
                        Id = cat.Id.ToString(),
                        Name = cat.Name,
                        IsSelected = false,
                        Color = "#EA580C"
                    };

                    if (cat.SubCategories != null && cat.SubCategories.Count > 0)
                    {
                        foreach (var sub in cat.SubCategories)
                        {
                            rootCatItem.SubCategories.Add(new PosCategoryItem
                            {
                                Id = sub.Id.ToString(),
                                ParentId = cat.Id.ToString(),
                                Name = sub.Name,
                                IsSelected = false,
                                Color = "#EA580C"
                            });
                        }
                    }

                    Categories.Add(rootCatItem);
                }
            }

            // 2. Tải danh sách tồn kho & sản phẩm từ API
            await FetchProductsFromApiAsync();
            StatusText = "Sẵn sàng";
        }
        catch (Exception ex)
        {
            StatusText = $"Lỗi kết nối API: {ex.Message}";
            FilteredProducts.Clear();
        }
        finally
        {
            IsLoadingCatalog = false;
        }
    }

    private async Task FetchProductsFromApiAsync()
    {
        Guid? catId = null;
        // Ưu tiên lọc theo danh mục con nếu có chọn
        if (SelectedSubCategory != null && SelectedSubCategory.Id != "ALL" && Guid.TryParse(SelectedSubCategory.Id, out var parsedSubCatId))
        {
            catId = parsedSubCatId;
        }
        else if (SelectedCategory != null && SelectedCategory.Id != "ALL" && Guid.TryParse(SelectedCategory.Id, out var parsedCatId))
        {
            catId = parsedCatId;
        }

        var stockRes = await _inventoryApiClient.GetStockAsync(
            categoryId: catId,
            search: string.IsNullOrWhiteSpace(SearchQuery) ? null : SearchQuery.Trim(),
            pageNumber: 1,
            pageSize: 100);

        FilteredProducts.Clear();

        if (stockRes?.Success == true && stockRes.Data?.Items != null)
        {
            int index = 0;
            foreach (var item in stockRes.Data.Items)
            {
                var (bgTint, iconFg) = GetColorPalette(index++);
                var productItem = new PosProductItem
                {
                    Id = item.SkuId,
                    Name = item.ProductName,
                    Specification = item.SkuCode,
                    Sku = item.SkuCode,
                    Barcode = item.Barcode,
                    Unit = "Cái",
                    Price = 0,
                    StockQuantity = (int)item.QtyOnHand,
                    CategoryId = item.SkuCode,
                    CategoryName = "Sản phẩm",
                    BackgroundTint = bgTint,
                    IconForeground = iconFg,
                    IconSymbol = GetIconSymbol(item.ProductName)
                };
                FilteredProducts.Add(productItem);
            }
        }
    }

    partial void OnSearchQueryChanged(string value)
    {
        _searchCts?.Cancel();
        _searchCts = new CancellationTokenSource();
        var token = _searchCts.Token;

        Task.Delay(300, token).ContinueWith(async t =>
        {
            if (!t.IsCanceled)
            {
                await App.Current.Dispatcher.InvokeAsync(async () =>
                {
                    await FetchProductsFromApiAsync();
                });
            }
        }, TaskScheduler.Default);
    }

    [RelayCommand]
    private void ClearSearch()
    {
        SearchQuery = string.Empty;
    }

    [RelayCommand]
    private void SelectCategory(PosCategoryItem category)
    {
        foreach (var c in Categories)
        {
            c.IsSelected = (c.Id == category.Id);
        }
        SelectedCategory = category;

        // Cập nhật danh sách danh mục con
        SubCategories.Clear();
        if (category.HasSubCategories)
        {
            var allSubItem = new PosCategoryItem
            {
                Id = category.Id,
                ParentId = category.Id,
                Name = "Tất cả",
                IsSelected = true,
                Color = "#EA580C"
            };
            SubCategories.Add(allSubItem);
            SelectedSubCategory = allSubItem;

            foreach (var sub in category.SubCategories)
            {
                sub.IsSelected = false;
                SubCategories.Add(sub);
            }
            HasSubCategories = true;
        }
        else
        {
            SelectedSubCategory = null;
            HasSubCategories = false;
        }

        _ = FetchProductsFromApiAsync();
    }

    [RelayCommand]
    private void SelectSubCategory(PosCategoryItem subCategory)
    {
        foreach (var s in SubCategories)
        {
            s.IsSelected = (s.Id == subCategory.Id);
        }
        SelectedSubCategory = subCategory;
        _ = FetchProductsFromApiAsync();
    }

    private static readonly (string bg, string fg)[] Palette = new[]
    {
        ("#FEE2E2", "#DC2626"),
        ("#DBEAFE", "#2563EB"),
        ("#DCFCE7", "#16A34A"),
        ("#FEF9C3", "#CA8A04"),
        ("#F3E8FF", "#9333EA"),
        ("#FFEDD5", "#EA580C"),
        ("#CCFBF1", "#0D9488"),
        ("#FCE7F3", "#DB2777")
    };

    private static (string bg, string fg) GetColorPalette(int index)
    {
        return Palette[Math.Abs(index) % Palette.Length];
    }

    private static SymbolRegular GetIconSymbol(string? productName)
    {
        var text = (productName ?? string.Empty).ToLowerInvariant();
        if (text.Contains("uống") || text.Contains("nước") || text.Contains("cà phê") || text.Contains("trà") || text.Contains("cola") || text.Contains("pepsi"))
            return SymbolRegular.DrinkBeer24;
        if (text.Contains("bánh") || text.Contains("kẹo") || text.Contains("snack"))
            return SymbolRegular.Gift24;
        if (text.Contains("thực phẩm") || text.Contains("mì") || text.Contains("mắm") || text.Contains("gạo") || text.Contains("ăn"))
            return SymbolRegular.Food24;
        if (text.Contains("mỹ phẩm") || text.Contains("dầu gội") || text.Contains("kem") || text.Contains("giấy") || text.Contains("rửa"))
            return SymbolRegular.Sparkle24;
        return SymbolRegular.Box24;
    }
}
