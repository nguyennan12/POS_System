using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using POS.Contracts.V1.Inventory;

namespace POS.WinUI.ViewModels.Inventory;

public sealed partial class InventoryViewModel
{
    // ── Danh sách các đợt kiểm kê kho ──
    public ObservableCollection<StockTakeItemModel> StockTakes { get; } = new();

    public ObservableCollection<string> StockTakeStatusOptions { get; } = new()
    {
        "Tất cả trạng thái",
        "Draft",
        "Pending",
        "Approved"
    };

    [ObservableProperty]
    private string _selectedStockTakeStatus = "Tất cả trạng thái";

    // ── Phân trang kiểm kê ──
    [ObservableProperty]
    private int _stockTakePage = 1;

    [ObservableProperty]
    private int _stockTakePageSize = 20;

    [ObservableProperty]
    private int _stockTakeTotalPages = 1;

    [ObservableProperty]
    private int _stockTakeTotalCount = 0;

    partial void OnSelectedStockTakeStatusChanged(string value)
    {
        StockTakePage = 1;
        _ = LoadStockTakesAsync();
    }

    // ── Modal Phiếu Kiểm Kê ──
    [ObservableProperty]
    private bool _isStockTakeModalOpen = false;

    [ObservableProperty]
    private Guid? _currentStockTakeId = null;

    [ObservableProperty]
    private string _currentStockTakeStatus = "Draft";

    [ObservableProperty]
    private string _stockTakeNote = string.Empty;

    [ObservableProperty]
    private string _stockTakeBarcodeQuery = string.Empty;

    public ObservableCollection<StockTakeLineItemModel> StockTakeLines { get; } = new();

    public decimal TotalSystemQty => StockTakeLines.Sum(l => l.SystemQty);
    public decimal TotalActualQty => StockTakeLines.Sum(l => l.ActualQty);
    public decimal TotalDiffQty => StockTakeLines.Sum(l => l.DiffQty);

    public bool CanEditStockTake => CurrentStockTakeStatus == "Draft";
    public bool CanSubmitStockTake => CurrentStockTakeStatus == "Draft" && StockTakeLines.Count > 0;
    public bool CanApproveStockTake => CurrentStockTakeStatus == "Pending" && (IsOwner || IsStoreManager);

    // ── Tải danh sách đợt kiểm kê ──
    [RelayCommand]
    public async Task LoadStockTakesAsync(CancellationToken ct = default)
    {
        IsLoading = true;
        try
        {
            var statusParam = SelectedStockTakeStatus == "Tất cả trạng thái" ? null : SelectedStockTakeStatus;
            var res = await _inventoryApi.GetStockTakesAsync(
                storeId: null,
                status: statusParam,
                from: null,
                to: null,
                pageNumber: StockTakePage,
                pageSize: StockTakePageSize,
                ct: ct);

            StockTakes.Clear();
            if (res?.Data != null)
            {
                StockTakeTotalCount = res.Data.TotalCount;
                StockTakeTotalPages = Math.Max(1, (int)Math.Ceiling((double)StockTakeTotalCount / StockTakePageSize));

                foreach (var st in res.Data.Items)
                {
                    StockTakes.Add(StockTakeItemModel.FromDto(st));
                }

                OpenStockTakesCount = StockTakes.Count(st => st.Status == "Draft" || st.Status == "Pending");
            }
        }
        catch (Exception ex)
        {
            ShowNotification($"Lỗi tải danh sách kiểm kê: {ex.Message}", true);
        }
        finally
        {
            IsLoading = false;
        }
    }

    // ── Mở Modal Tạo Đợt Kiểm Kê Mới ──
    [RelayCommand]
    public async Task OpenCreateStockTakeModalAsync()
    {
        IsLoading = true;
        try
        {
            var note = $"Kiểm kê kho ngày {DateTime.Now:dd/MM/yyyy HH:mm}";
            var createRes = await _inventoryApi.CreateStockTakeAsync(new CreateStockTakeRequest(note));

            if (createRes?.Data != null)
            {
                var detail = createRes.Data;
                CurrentStockTakeId = detail.Id;
                CurrentStockTakeStatus = detail.Status;
                StockTakeNote = detail.Note ?? note;
                StockTakeBarcodeQuery = string.Empty;

                StockTakeLines.Clear();
                foreach (var item in detail.Items)
                {
                    var line = StockTakeLineItemModel.FromDto(item);
                    line.PropertyChanged += (s, e) => UpdateStockTakeSummary();
                    StockTakeLines.Add(line);
                }

                UpdateStockTakeSummary();
                IsStockTakeModalOpen = true;
            }
        }
        catch (Exception ex)
        {
            ShowNotification($"Lỗi khởi tạo đợt kiểm kê: {ex.Message}", true);
        }
        finally
        {
            IsLoading = false;
        }
    }

    // ── Mở Chi Tiết Đợt Kiểm Kê ──
    [RelayCommand]
    public async Task OpenStockTakeDetailModalAsync(StockTakeItemModel? item)
    {
        if (item == null) return;
        IsLoading = true;
        try
        {
            var res = await _inventoryApi.GetStockTakeByIdAsync(item.Id);
            if (res?.Data != null)
            {
                var detail = res.Data;
                CurrentStockTakeId = detail.Id;
                CurrentStockTakeStatus = detail.Status;
                StockTakeNote = detail.Note ?? string.Empty;
                StockTakeBarcodeQuery = string.Empty;

                StockTakeLines.Clear();
                foreach (var lineItem in detail.Items)
                {
                    var line = StockTakeLineItemModel.FromDto(lineItem);
                    line.PropertyChanged += (s, e) => UpdateStockTakeSummary();
                    StockTakeLines.Add(line);
                }

                UpdateStockTakeSummary();
                IsStockTakeModalOpen = true;
            }
        }
        catch (Exception ex)
        {
            ShowNotification($"Lỗi tải chi tiết kiểm kê: {ex.Message}", true);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public void CloseStockTakeModal()
    {
        IsStockTakeModalOpen = false;
    }

    // ── Quét mã vạch kiểm kê đếm nhanh ──
    [RelayCommand]
    public void ScanBarcodeInStockTake()
    {
        var query = StockTakeBarcodeQuery?.Trim();
        if (string.IsNullOrWhiteSpace(query)) return;

        var line = StockTakeLines.FirstOrDefault(l =>
            string.Equals(l.Barcode, query, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(l.SkuCode, query, StringComparison.OrdinalIgnoreCase));

        if (line != null)
        {
            line.ActualQty += 1;
            StockTakeBarcodeQuery = string.Empty;
            UpdateStockTakeSummary();
            ShowNotification($"Đã tăng số đếm thực tế của {line.ProductName} lên {line.ActualQty}.");
        }
        else
        {
            ShowNotification($"Không tìm thấy mặt hàng có mã '{query}' trong đợt kiểm kê này.", true);
        }
    }

    private void UpdateStockTakeSummary()
    {
        OnPropertyChanged(nameof(TotalSystemQty));
        OnPropertyChanged(nameof(TotalActualQty));
        OnPropertyChanged(nameof(TotalDiffQty));
        OnPropertyChanged(nameof(CanEditStockTake));
        OnPropertyChanged(nameof(CanSubmitStockTake));
        OnPropertyChanged(nameof(CanApproveStockTake));
    }

    // ── Lưu kết quả đếm thực tế ──
    [RelayCommand]
    public async Task SaveStockTakeItemsAsync()
    {
        if (!CurrentStockTakeId.HasValue) return;

        IsLoading = true;
        try
        {
            var updateItems = StockTakeLines.Select(l => new StockTakeItemUpdateRequest(
                l.SkuId,
                l.ActualQty,
                l.Note
            )).ToList();

            var req = new UpdateStockTakeItemsRequest(updateItems);
            var res = await _inventoryApi.UpdateStockTakeItemsAsync(CurrentStockTakeId.Value, req);

            if (res != null)
            {
                ShowNotification("Đã lưu kết quả kiểm đếm thực tế.");
                await LoadStockTakesAsync();
            }
        }
        catch (Exception ex)
        {
            ShowNotification($"Lỗi lưu kiểm đếm: {ex.Message}", true);
        }
        finally
        {
            IsLoading = false;
        }
    }

    // ── Gửi duyệt kiểm kê (Draft -> Pending) ──
    [RelayCommand]
    public async Task SubmitStockTakeAsync()
    {
        if (!CurrentStockTakeId.HasValue) return;

        IsLoading = true;
        try
        {
            // Tự động lưu kiểm đếm trước khi submit
            var updateItems = StockTakeLines.Select(l => new StockTakeItemUpdateRequest(
                l.SkuId,
                l.ActualQty,
                l.Note
            )).ToList();

            await _inventoryApi.UpdateStockTakeItemsAsync(CurrentStockTakeId.Value, new UpdateStockTakeItemsRequest(updateItems));

            var res = await _inventoryApi.SubmitStockTakeAsync(CurrentStockTakeId.Value);
            if (res != null)
            {
                CurrentStockTakeStatus = "Pending";
                UpdateStockTakeSummary();
                ShowNotification("Đã gửi phiếu kiểm kê chờ Quản lý phê duyệt.");
                await LoadStockTakesAsync();
            }
        }
        catch (Exception ex)
        {
            ShowNotification($"Lỗi gửi duyệt: {ex.Message}", true);
        }
        finally
        {
            IsLoading = false;
        }
    }

    // ── Quản lý Phê duyệt cân bằng kho (Pending -> Approved) ──
    [RelayCommand]
    public async Task ApproveStockTakeAsync()
    {
        if (!CurrentStockTakeId.HasValue) return;

        if (!IsOwner && !IsStoreManager)
        {
            ShowNotification("Chỉ Quản lý hoặc Chủ cửa hàng mới có quyền phê duyệt cân bằng kho.", true);
            return;
        }

        IsLoading = true;
        try
        {
            var res = await _inventoryApi.ApproveStockTakeAsync(CurrentStockTakeId.Value);
            if (res != null)
            {
                CurrentStockTakeStatus = "Approved";
                UpdateStockTakeSummary();
                ShowNotification("Đã phê duyệt kiểm kê! Tồn kho hệ thống đã được tự động cân bằng (Adjust).");
                IsStockTakeModalOpen = false;
                await LoadStockTakesAsync();
                await LoadStockEntriesAsync();
                await LoadStockAlertsAsync();
            }
        }
        catch (Exception ex)
        {
            ShowNotification($"Lỗi phê duyệt kiểm kê: {ex.Message}", true);
        }
        finally
        {
            IsLoading = false;
        }
    }
}
