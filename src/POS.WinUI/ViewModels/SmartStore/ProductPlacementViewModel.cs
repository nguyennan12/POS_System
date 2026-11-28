using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using POS.Contracts.V1.StoreLayouts;
using POS.WinUI.Core.ApiClients;
using POS.WinUI.Core.Services;
using POS.WinUI.ViewModels.SmartStore.Models;

namespace POS.WinUI.ViewModels.SmartStore;

public partial class ProductPlacementViewModel : ObservableObject
{
    private readonly ProductPlacementApiClient _placementClient;
    private readonly InventoryApiClient _inventoryClient;
    private readonly ProductApiClient _productClient;
    private readonly SessionService _session;

    [ObservableProperty] private Guid _layoutId;
    [ObservableProperty] private int _layoutRevision;
    [ObservableProperty] private bool _canEdit = true;
    [ObservableProperty] private string _barcodeInput = string.Empty;
    [ObservableProperty] private bool _isLookingUp;
    [ObservableProperty] private bool _isSaving;
    [ObservableProperty] private string? _errorMessage;
    [ObservableProperty] private string? _successMessage;

    // Scanned product details
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasScannedSku))]
    private Guid? _scannedSkuId;

    public bool HasScannedSku => ScannedSkuId.HasValue;
    [ObservableProperty] private string? _scannedSkuCode;
    [ObservableProperty] private string? _scannedProductName;
    [ObservableProperty] private string? _scannedBarcode;
    [ObservableProperty] private decimal _scannedSellPrice;
    [ObservableProperty] private decimal _scannedStockQty;

    // Shelf & Level target
    [ObservableProperty] private ShelfModel? _selectedShelf;
    [ObservableProperty] private ShelfLevelModel? _selectedShelfLevel;
    [ObservableProperty] private bool _isPrimary = true;
    [ObservableProperty] private string? _placementNote;
    [ObservableProperty] private string _selectedAction = "Add"; // "Add" or "Move"

    public ObservableCollection<ShelfModel> AvailableShelves { get; } = [];
    public ObservableCollection<ShelfLevelModel> AvailableLevels { get; } = [];
    public ObservableCollection<ProductLocationItemResponse> ExistingLocations { get; } = [];
    public ObservableCollection<ShelfPlacementDisplayItem> ShelfPlacements { get; } = [];

    public event Action<int>? PlacementMutated;

    public ProductPlacementViewModel(
        ProductPlacementApiClient placementClient,
        InventoryApiClient inventoryClient,
        ProductApiClient productClient,
        SessionService session)
    {
        _placementClient = placementClient;
        _inventoryClient = inventoryClient;
        _productClient = productClient;
        _session = session;
    }

    public void Initialize(StoreLayoutDetailModel layout, ShelfModel? preselectedShelf = null)
    {
        LayoutId = layout.Id;
        LayoutRevision = layout.Revision;
        CanEdit = layout.IsDraft;

        AvailableShelves.Clear();
        foreach (var shelf in layout.Shelves)
        {
            AvailableShelves.Add(shelf);
        }

        SelectedShelf = preselectedShelf ?? AvailableShelves.FirstOrDefault();
        UpdateAvailableLevels();
        ClearForm();

        if (!CanEdit)
        {
            ErrorMessage = "Mặt bằng đang hoạt động được bảo vệ ở chế độ Chỉ đọc. Vui lòng chuyển sang một Bản thảo (Draft) hoặc nhân bản mặt bằng để gán vị trí SKU.";
        }
    }

    partial void OnSelectedShelfChanged(ShelfModel? value)
    {
        UpdateAvailableLevels();
        if (value != null)
        {
            _ = LoadShelfContentsAsync(value.Id);
        }
    }

    private void UpdateAvailableLevels()
    {
        AvailableLevels.Clear();
        if (SelectedShelf != null)
        {
            foreach (var lvl in SelectedShelf.Levels)
            {
                AvailableLevels.Add(lvl);
            }
        }
        SelectedShelfLevel = AvailableLevels.FirstOrDefault();
    }

    public async Task LoadShelfContentsAsync(Guid shelfId)
    {
        try
        {
            ShelfPlacements.Clear();
            var res = await _placementClient.GetShelfContentsAsync(shelfId);
            if (res?.Success == true && res.Data != null)
            {
                foreach (var lvl in res.Data.Levels)
                {
                    foreach (var sku in lvl.SkuPlacements)
                    {
                        ShelfPlacements.Add(new ShelfPlacementDisplayItem(
                            sku.PlacementId,
                            sku.SkuId,
                            sku.SkuCode,
                            sku.ProductName,
                            sku.Barcode,
                            sku.IsPrimary,
                            sku.Note,
                            lvl.LevelNumber,
                            lvl.Label));
                    }
                }
            }
        }
        catch
        {
            // Ignore background refresh errors
        }
    }

    public void PreselectShelf(ShelfModel shelf)
    {
        var match = AvailableShelves.FirstOrDefault(s => s.Id == shelf.Id);
        if (match != null)
        {
            SelectedShelf = match;
        }
        else
        {
            AvailableShelves.Add(shelf);
            SelectedShelf = shelf;
        }
    }

    [RelayCommand]
    public async Task LookupBarcodeAsync()
    {
        var code = BarcodeInput.Trim();
        if (string.IsNullOrWhiteSpace(code)) return;

        IsLookingUp = true;
        ErrorMessage = null;
        SuccessMessage = null;

        try
        {
            var storeGuid = Guid.TryParse(_session.StoreId, out var g) ? g : (Guid?)null;

            // 1. Try finding in Inventory stock first
            var res = await _inventoryClient.GetStockAsync(search: code, pageNumber: 1, pageSize: 5, storeId: storeGuid);
            if (res?.Success == true && res.Data?.Items != null && res.Data.Items.Count > 0)
            {
                var match = res.Data.Items.FirstOrDefault(i => i.Barcode == code || i.SkuCode == code) ?? res.Data.Items[0];
                ScannedSkuId = match.SkuId;
                ScannedSkuCode = match.SkuCode;
                ScannedProductName = match.ProductName;
                ScannedBarcode = match.Barcode;
                ScannedSellPrice = 0;
                ScannedStockQty = match.QtyOnHand;

                await LoadExistingLocationsAsync(match.SkuId);
                return;
            }

            // 2. Fallback: Search in Catalog
            var catRes = await _productClient.GetPosCatalogAsync(search: code, pageNumber: 1, pageSize: 5, storeId: storeGuid);
            if (catRes?.Success == true && catRes.Data?.Items != null && catRes.Data.Items.Count > 0)
            {
                var match = catRes.Data.Items.FirstOrDefault(i => i.Barcode == code || i.SkuCode == code) ?? catRes.Data.Items[0];
                ScannedSkuId = match.SkuId;
                ScannedSkuCode = match.SkuCode;
                ScannedProductName = match.ProductName;
                ScannedBarcode = match.Barcode;
                ScannedSellPrice = match.SellPrice;
                ScannedStockQty = match.QtyOnHand;

                await LoadExistingLocationsAsync(match.SkuId);
                return;
            }

            // 3. Fallback without store filter in case store mismatch
            var allStockRes = await _inventoryClient.GetStockAsync(search: code, pageNumber: 1, pageSize: 5);
            if (allStockRes?.Success == true && allStockRes.Data?.Items != null && allStockRes.Data.Items.Count > 0)
            {
                var match = allStockRes.Data.Items.FirstOrDefault(i => i.Barcode == code || i.SkuCode == code) ?? allStockRes.Data.Items[0];
                ScannedSkuId = match.SkuId;
                ScannedSkuCode = match.SkuCode;
                ScannedProductName = match.ProductName;
                ScannedBarcode = match.Barcode;
                ScannedSellPrice = 0;
                ScannedStockQty = match.QtyOnHand;

                await LoadExistingLocationsAsync(match.SkuId);
                return;
            }

            ErrorMessage = $"Không tìm thấy sản phẩm có mã hoặc barcode: {code}";
            ScannedSkuId = null;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Lỗi tra cứu: {ex.Message}";
        }
        finally
        {
            IsLookingUp = false;
        }
    }

    private async Task LoadExistingLocationsAsync(Guid skuId)
    {
        ExistingLocations.Clear();
        try
        {
            var locRes = await _placementClient.GetBySkuAsync(skuId);
            if (locRes?.Success == true && locRes.Data != null)
            {
                foreach (var loc in locRes.Data.Locations)
                {
                    ExistingLocations.Add(loc);
                }
            }
        }
        catch
        {
            // Ignore
        }
    }

    [RelayCommand]
    public async Task AssignPlacementAsync()
    {
        if (!CanEdit)
        {
            ErrorMessage = "Chỉ có thể gán vị trí trên mặt bằng Bản thảo (Draft). Vui lòng nhân bản thành bản thảo để sửa.";
            return;
        }

        if (ScannedSkuId == null || SelectedShelfLevel == null)
        {
            ErrorMessage = "Vui lòng quét mã sản phẩm và chọn tầng kệ cần đặt.";
            return;
        }

        IsSaving = true;
        ErrorMessage = null;
        SuccessMessage = null;

        try
        {
            var req = new AssignProductPlacementRequest(
                LayoutId,
                SelectedShelfLevel.Id,
                ScannedSkuId.Value,
                IsPrimary,
                PlacementNote,
                LayoutRevision);

            var res = await _placementClient.AssignAsync(req);
            if (res?.Success == true && res.Data != null)
            {
                LayoutRevision = res.Data.LayoutRevision;
                SuccessMessage = $"Đã gán thành công '{ScannedProductName}' vào {SelectedShelf?.Code} - Tầng {SelectedShelfLevel.LevelNumber}!";
                PlacementMutated?.Invoke(LayoutRevision);

                if (SelectedShelf != null)
                {
                    await LoadShelfContentsAsync(SelectedShelf.Id);
                }
                await LoadExistingLocationsAsync(ScannedSkuId.Value);
                BarcodeInput = string.Empty;
            }
            else
            {
                ErrorMessage = res?.Error?.Message ?? "Không thể gán vị trí sản phẩm.";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsSaving = false;
        }
    }

    [RelayCommand]
    public Task SavePlacementAsync() => AssignPlacementAsync();

    [RelayCommand]
    public async Task DeletePlacementAsync(object? placement)
    {
        if (placement == null) return;
        Guid placementId = Guid.Empty;
        string productName = "sản phẩm";

        if (placement is ShelfPlacementDisplayItem displayItem)
        {
            placementId = displayItem.PlacementId;
            productName = displayItem.ProductName;
        }
        else if (placement is ShelfSkuContentResponse response)
        {
            placementId = response.PlacementId;
            productName = response.ProductName;
        }

        if (placementId == Guid.Empty) return;

        IsSaving = true;
        ErrorMessage = null;

        try
        {
            var res = await _placementClient.RemoveAsync(placementId, LayoutId, LayoutRevision);
            if (res?.Success == true && res.Data != null)
            {
                LayoutRevision = res.Data.NewRevision;
                SuccessMessage = $"Đã gỡ '{productName}' khỏi kệ.";
                PlacementMutated?.Invoke(LayoutRevision);

                if (SelectedShelf != null)
                {
                    await LoadShelfContentsAsync(SelectedShelf.Id);
                }
                if (ScannedSkuId.HasValue)
                {
                    await LoadExistingLocationsAsync(ScannedSkuId.Value);
                }
            }
            else
            {
                ErrorMessage = res?.Error?.Message ?? "Không thể xóa vị trí.";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsSaving = false;
        }
    }

    [RelayCommand]
    public void ClearForm()
    {
        BarcodeInput = string.Empty;
        ScannedSkuId = null;
        ScannedSkuCode = null;
        ScannedProductName = null;
        ScannedBarcode = null;
        ScannedSellPrice = 0;
        ScannedStockQty = 0;
        PlacementNote = null;
        ErrorMessage = null;
        SuccessMessage = null;
        ExistingLocations.Clear();
    }
}

public sealed record ShelfPlacementDisplayItem(
    Guid PlacementId,
    Guid SkuId,
    string SkuCode,
    string ProductName,
    string? Barcode,
    bool IsPrimary,
    string? Note,
    int LevelNumber,
    string? LevelLabel)
{
    public string DisplayLevel => string.IsNullOrWhiteSpace(LevelLabel)
        ? $"Tầng {LevelNumber}"
        : $"Tầng {LevelNumber} ({LevelLabel})";
}
