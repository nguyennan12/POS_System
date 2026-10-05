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

        var search = string.IsNullOrWhiteSpace(SearchQuery) ? null : SearchQuery.Trim();

        FilteredProducts.Clear();

        // 1. Gọi 1 API duy nhất tối ưu cho POS Catalog (đã có đủ SKU, Giá bán, Tồn kho, Mã vạch)
        var catalogRes = await _productApiClient.GetPosCatalogAsync(
            categoryId: catId,
            search: search,
            pageNumber: 1,
            pageSize: 100);

        if (catalogRes?.Success == true && catalogRes.Data?.Items != null && catalogRes.Data.Items.Count > 0)
        {
            foreach (var item in catalogRes.Data.Items)
            {
                var unitText = !string.IsNullOrWhiteSpace(item.BaseUnit) ? item.BaseUnit : "Cái";

                FilteredProducts.Add(new PosProductItem
                {
                    Id = item.SkuId,
                    Name = item.ProductName,
                    Specification = unitText,
                    Sku = item.SkuCode,
                    Barcode = item.Barcode,
                    Unit = unitText,
                    Price = item.SellPrice,
                    StockQuantity = (int)item.QtyOnHand,
                    CategoryId = item.CategoryId.ToString(),
                    CategoryName = item.CategoryName,
                    ImageUrl = item.ImageUrl
                });
            }
        }
        else
        {
            // 2. Fallback an toàn nếu chưa có bản ghi SKU/Product
            var stockRes = await _inventoryApiClient.GetStockAsync(
                categoryId: catId,
                search: search,
                pageNumber: 1,
                pageSize: 100);

            if (stockRes?.Success == true && stockRes.Data?.Items != null)
            {
                foreach (var item in stockRes.Data.Items)
                {
                    FilteredProducts.Add(new PosProductItem
                    {
                        Id = item.SkuId,
                        Name = item.ProductName,
                        Specification = "Cái",
                        Sku = item.SkuCode,
                        Barcode = item.Barcode,
                        Unit = "Cái",
                        Price = 0,
                        StockQuantity = (int)item.QtyOnHand,
                        CategoryId = item.SkuCode,
                        CategoryName = "Sản phẩm"
                    });
                }
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
}
