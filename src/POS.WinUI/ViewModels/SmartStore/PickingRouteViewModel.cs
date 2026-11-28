using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using POS.Contracts.V1.StoreLayouts;
using POS.WinUI.Core.ApiClients;
using POS.WinUI.ViewModels.SmartStore.Models;

namespace POS.WinUI.ViewModels.SmartStore;

public partial class PickingRouteItemModel : ObservableObject
{
    public Guid SkuId { get; set; }
    public string SkuCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string? Barcode { get; set; }
    [ObservableProperty] private decimal _quantity = 1;
}

public sealed record PlacedProductOption(
    Guid SkuId,
    string SkuCode,
    string ProductName,
    string? Barcode,
    string ShelfCode,
    string ShelfName,
    int LevelNumber)
{
    public string DisplayText => $"{ProductName} ({ShelfCode} - Tầng {LevelNumber})";
}

public partial class PickingRouteViewModel : ObservableObject
{
    private readonly StoreLayoutApiClient _layoutClient;
    private readonly InventoryApiClient _inventoryClient;
    private readonly ProductApiClient _productClient;
    private readonly ProductPlacementApiClient _placementClient;

    [ObservableProperty] private Guid? _currentLayoutId;
    [ObservableProperty] private string _currentLayoutName = string.Empty;
    [ObservableProperty] private bool _isCalculating;
    [ObservableProperty] private string? _errorMessage;
    [ObservableProperty] private string? _routeNotice;
    [ObservableProperty] private string _searchKeyword = string.Empty;
    [ObservableProperty] private bool _hasMissingItems;
    [ObservableProperty] private PlacedProductOption? _selectedPlacedProduct;

    public ObservableCollection<PlacedProductOption> AvailablePlacedProducts { get; } = [];
    public ObservableCollection<PickingRouteItemModel> SelectedItems { get; } = [];
    public ObservableCollection<PickingRouteStopResponse> OrderedStops { get; } = [];
    public ObservableCollection<Guid> MissingSkuIds { get; } = [];
    public ObservableCollection<Guid> UnreachableSkuIds { get; } = [];

    public event Action<PickingRoutePreviewResponse>? RouteCalculated;
    public event Action? RouteCleared;

    public PickingRouteViewModel(
        StoreLayoutApiClient layoutClient,
        InventoryApiClient inventoryClient,
        ProductApiClient productClient,
        ProductPlacementApiClient placementClient)
    {
        _layoutClient = layoutClient;
        _inventoryClient = inventoryClient;
        _productClient = productClient;
        _placementClient = placementClient;
    }

    public void Initialize(StoreLayoutDetailModel layout)
    {
        _ = LoadPlacedProductsAsync(layout);
    }

    public async Task LoadPlacedProductsAsync(StoreLayoutDetailModel layout)
    {
        CurrentLayoutId = layout.Id;
        CurrentLayoutName = layout.Name;
        AvailablePlacedProducts.Clear();

        try
        {
            foreach (var shelf in layout.Shelves)
            {
                var res = await _placementClient.GetShelfContentsAsync(shelf.Id);
                if (res?.Success == true && res.Data != null)
                {
                    foreach (var lvl in res.Data.Levels)
                    {
                        foreach (var p in lvl.SkuPlacements)
                        {
                            if (!AvailablePlacedProducts.Any(x => x.SkuId == p.SkuId && x.ShelfCode == shelf.Code && x.LevelNumber == lvl.LevelNumber))
                            {
                                AvailablePlacedProducts.Add(new PlacedProductOption(
                                    p.SkuId,
                                    p.SkuCode,
                                    p.ProductName,
                                    p.Barcode,
                                    shelf.Code,
                                    shelf.Name,
                                    lvl.LevelNumber));
                            }
                        }
                    }
                }
            }
        }
        catch
        {
            // Ignore background fetch errors
        }
    }

    partial void OnSelectedPlacedProductChanged(PlacedProductOption? value)
    {
        if (value == null) return;
        AddOrIncrementItem(value.SkuId, value.SkuCode, value.ProductName, value.Barcode);
        SelectedPlacedProduct = null;
        ErrorMessage = null;
    }

    [RelayCommand]
    public void AddAllPlacedItems()
    {
        if (AvailablePlacedProducts.Count == 0) return;
        foreach (var p in AvailablePlacedProducts)
        {
            AddOrIncrementItem(p.SkuId, p.SkuCode, p.ProductName, p.Barcode);
        }
        ErrorMessage = null;
    }

    [RelayCommand]
    public async Task SearchAndAddItemAsync()
    {
        var kw = SearchKeyword.Trim();
        if (string.IsNullOrWhiteSpace(kw)) return;

        try
        {
            // 1. Tìm trong sản phẩm đã gán trên kệ mặt bằng hiện tại trước (Khớp nhanh nhất)
            var placedMatch = AvailablePlacedProducts.FirstOrDefault(p =>
                string.Equals(p.Barcode, kw, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(p.SkuCode, kw, StringComparison.OrdinalIgnoreCase) ||
                p.ProductName.Contains(kw, StringComparison.OrdinalIgnoreCase));

            if (placedMatch != null)
            {
                AddOrIncrementItem(placedMatch.SkuId, placedMatch.SkuCode, placedMatch.ProductName, placedMatch.Barcode);
                SearchKeyword = string.Empty;
                ErrorMessage = null;
                return;
            }

            // 2. Tìm trong Danh mục POS Sản phẩm
            var catRes = await _productClient.GetPosCatalogAsync(search: kw, pageNumber: 1, pageSize: 5);
            if (catRes?.Success == true && catRes.Data?.Items != null && catRes.Data.Items.Count > 0)
            {
                var match = catRes.Data.Items.FirstOrDefault(i => string.Equals(i.Barcode, kw, StringComparison.OrdinalIgnoreCase)) ?? catRes.Data.Items[0];
                AddOrIncrementItem(match.SkuId, match.SkuCode, match.ProductName, match.Barcode);
                SearchKeyword = string.Empty;
                ErrorMessage = null;
                return;
            }

            // 3. Tìm theo mã vạch trực tiếp
            var barcodeRes = await _productClient.GetSkuByBarcodeAsync(kw);
            if (barcodeRes?.Success == true && barcodeRes.Data != null)
            {
                AddOrIncrementItem(barcodeRes.Data.Id, barcodeRes.Data.SkuCode, barcodeRes.Data.ProductName, barcodeRes.Data.Barcode);
                SearchKeyword = string.Empty;
                ErrorMessage = null;
                return;
            }

            // 4. Tìm trong Kho tồn kho
            var res = await _inventoryClient.GetStockAsync(search: kw, pageNumber: 1, pageSize: 5);
            if (res?.Success == true && res.Data?.Items != null && res.Data.Items.Count > 0)
            {
                var match = res.Data.Items.FirstOrDefault(i => i.Barcode == kw) ?? res.Data.Items[0];
                AddOrIncrementItem(match.SkuId, match.SkuCode, match.ProductName, match.Barcode);
                SearchKeyword = string.Empty;
                ErrorMessage = null;
                return;
            }

            ErrorMessage = $"Không tìm thấy sản phẩm '{kw}'. Vui lòng thử từ khóa khác.";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    private void AddOrIncrementItem(Guid skuId, string skuCode, string productName, string? barcode)
    {
        var existing = SelectedItems.FirstOrDefault(i => i.SkuId == skuId);
        if (existing != null)
        {
            existing.Quantity += 1;
        }
        else
        {
            SelectedItems.Add(new PickingRouteItemModel
            {
                SkuId = skuId,
                SkuCode = skuCode,
                ProductName = productName,
                Barcode = barcode,
                Quantity = 1
            });
        }
    }

    [RelayCommand]
    public void RemoveItem(PickingRouteItemModel? item)
    {
        if (item != null)
        {
            SelectedItems.Remove(item);
        }
    }

    [RelayCommand]
    public async Task CalculateRouteAsync()
    {
        if (SelectedItems.Count == 0)
        {
            ErrorMessage = "Vui lòng thêm ít nhất một sản phẩm vào danh sách lấy hàng.";
            return;
        }

        IsCalculating = true;
        ErrorMessage = null;
        OrderedStops.Clear();
        MissingSkuIds.Clear();
        UnreachableSkuIds.Clear();

        try
        {
            var itemsReq = SelectedItems
                .Select(i => new PickingRouteItemRequest(i.SkuId, i.Quantity))
                .ToList();

            var req = new PreviewPickingRouteRequest(itemsReq);
            var res = await _layoutClient.PreviewRouteAsync(req, CurrentLayoutId);

            if (res?.Success == true && res.Data != null)
            {
                RouteNotice = res.Data.RouteNotice;

                foreach (var stop in res.Data.OrderedStops)
                {
                    OrderedStops.Add(stop);
                }

                foreach (var m in res.Data.MissingSkuIds)
                {
                    MissingSkuIds.Add(m);
                }

                foreach (var u in res.Data.UnreachableSkuIds)
                {
                    UnreachableSkuIds.Add(u);
                }

                HasMissingItems = MissingSkuIds.Count > 0 || UnreachableSkuIds.Count > 0;
                RouteCalculated?.Invoke(res.Data);
            }
            else
            {
                ErrorMessage = res?.Error?.Message ?? "Không thể tính toán tuyến đường nhặt hàng.";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsCalculating = false;
        }
    }

    [RelayCommand]
    public void ClearAll()
    {
        SelectedItems.Clear();
        OrderedStops.Clear();
        MissingSkuIds.Clear();
        UnreachableSkuIds.Clear();
        ErrorMessage = null;
        RouteNotice = null;
        HasMissingItems = false;
        RouteCleared?.Invoke();
    }
}
