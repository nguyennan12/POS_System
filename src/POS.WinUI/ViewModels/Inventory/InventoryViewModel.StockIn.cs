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
    // ── Danh sách phiếu nhập kho ──
    public ObservableCollection<StockInVoucherItemModel> StockInVouchers { get; } = new();
    public bool HasNoStockInVouchers => StockInVouchers.Count == 0 && !IsLoading;
    public bool HasStockInVouchers => StockInVouchers.Count > 0;

    public ObservableCollection<SupplierOptionModel> Suppliers { get; } = new();

    public ObservableCollection<string> VoucherStatusFilterOptions { get; } = new()
    {
        "Tất cả trạng thái",
        "Chờ duyệt",
        "Đã nhập kho",
        "Đã hủy"
    };

    [ObservableProperty]
    private string _selectedVoucherStatus = "Tất cả trạng thái";

    [ObservableProperty]
    private string _voucherSearchKeyword = string.Empty;

    [ObservableProperty]
    private SupplierOptionModel? _selectedVoucherSupplierFilter;

    [ObservableProperty]
    private DateTime? _voucherFromDate;

    [ObservableProperty]
    private DateTime? _voucherToDate;

    // ── Phân trang phiếu nhập ──
    [ObservableProperty]
    private int _voucherPage = 1;

    [ObservableProperty]
    private int _voucherPageSize = 15;

    public ObservableCollection<int> VoucherPageSizeOptions { get; } = new() { 15, 30, 50, 100 };

    partial void OnVoucherPageSizeChanged(int value)
    {
        VoucherPage = 1;
        _ = LoadStockInVouchersAsync();
    }

    [ObservableProperty]
    private int _voucherTotalPages = 1;

    [ObservableProperty]
    private int _voucherTotalCount = 0;

    public string VoucherPaginationSummary => VoucherTotalCount == 0
        ? "0 / 0 phiếu nhập"
        : $"Trang {VoucherPage} / {Math.Max(1, VoucherTotalPages)} (Tổng cộng {VoucherTotalCount} phiếu nhập)";

    public bool HasPreviousVoucherPage => VoucherPage > 1;
    public bool HasNextVoucherPage => VoucherPage < VoucherTotalPages;

    [RelayCommand]
    public async Task PreviousVoucherPageAsync()
    {
        if (HasPreviousVoucherPage)
        {
            VoucherPage--;
            await LoadStockInVouchersAsync();
        }
    }

    [RelayCommand]
    public async Task NextVoucherPageAsync()
    {
        if (HasNextVoucherPage)
        {
            VoucherPage++;
            await LoadStockInVouchersAsync();
        }
    }

    partial void OnSelectedVoucherStatusChanged(string value)
    {
        VoucherPage = 1;
        _ = LoadStockInVouchersAsync();
    }

    partial void OnVoucherPageChanged(int value)
    {
        OnPropertyChanged(nameof(VoucherPaginationSummary));
        OnPropertyChanged(nameof(HasPreviousVoucherPage));
        OnPropertyChanged(nameof(HasNextVoucherPage));
    }

    partial void OnVoucherTotalCountChanged(int value)
    {
        OnPropertyChanged(nameof(VoucherPaginationSummary));
        OnPropertyChanged(nameof(HasPreviousVoucherPage));
        OnPropertyChanged(nameof(HasNextVoucherPage));
    }

    partial void OnVoucherTotalPagesChanged(int value)
    {
        OnPropertyChanged(nameof(VoucherPaginationSummary));
        OnPropertyChanged(nameof(HasNextVoucherPage));
    }

    // ── Modal Phiếu Nhập Kho ──
    [ObservableProperty]
    private bool _isStockInModalOpen = false;

    [ObservableProperty]
    private Guid? _editingVoucherId = null;

    [ObservableProperty]
    private string _editingVoucherStatus = "Draft";

    [ObservableProperty]
    private SupplierOptionModel? _selectedSupplier;

    [ObservableProperty]
    private string _stockInNote = string.Empty;

    [ObservableProperty]
    private string _stockInBarcodeQuery = string.Empty;

    public ObservableCollection<StockInLineItemModel> StockInLines { get; } = new();

    public decimal TotalStockInAmount => StockInLines.Sum(l => l.TotalPrice);
    public string TotalStockInAmountFormatted => TotalStockInAmount.ToString("N0") + " đ";

    // New vouchers (EditingVoucherId == null) are always editable.
    // Existing saved vouchers are read-only regardless of status to prevent duplicate drafts.
    public bool CanEditStockIn => EditingVoucherId == null;
    public bool CanCompleteEditingVoucher => EditingVoucherId.HasValue && EditingVoucherStatus == "Draft";

    // ── Tải danh sách Nhà cung cấp ──
    public async Task LoadSuppliersAsync(CancellationToken ct = default)
    {
        try
        {
            var res = await _inventoryApi.GetSuppliersAsync(pageNumber: 1, pageSize: 100, ct: ct);
            if (res?.Data != null)
            {
                Suppliers.Clear();
                var allSup = new SupplierOptionModel { Id = Guid.Empty, Name = "Tất cả nhà cung cấp" };
                Suppliers.Add(allSup);
                foreach (var s in res.Data.Items)
                {
                    Suppliers.Add(SupplierOptionModel.FromDto(s));
                }
                if (SelectedVoucherSupplierFilter == null)
                {
                    SelectedVoucherSupplierFilter = allSup;
                }
                if (SelectedSupplier == null && Suppliers.Count > 1)
                {
                    SelectedSupplier = Suppliers[1];
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Inventory] Error loading suppliers: {ex.Message}");
        }
    }

    // ── Tải danh sách Phiếu nhập kho ──
    [RelayCommand]
    public async Task LoadStockInVouchersAsync(CancellationToken ct = default)
    {
        IsLoading = true;
        try
        {
            string? statusParam = SelectedVoucherStatus switch
            {
                "Chờ duyệt" or "Draft" => "Draft",
                "Đã nhập kho" or "Completed" => "Completed",
                "Đã hủy" or "Cancelled" => "Cancelled",
                _ => null
            };

            var supplierIdParam = SelectedVoucherSupplierFilter?.Id;
            var fromParam = VoucherFromDate.HasValue ? new DateTimeOffset(VoucherFromDate.Value) : (DateTimeOffset?)null;
            var toParam = VoucherToDate.HasValue ? new DateTimeOffset(VoucherToDate.Value.Date.AddDays(1).AddTicks(-1)) : (DateTimeOffset?)null;

            var storeIdParam = Guid.TryParse(CurrentStoreId, out var sId) && sId != Guid.Empty ? (Guid?)sId : null;
            var res = await _inventoryApi.GetStockInVouchersAsync(
                supplierId: supplierIdParam,
                storeId: storeIdParam,
                status: statusParam,
                from: fromParam,
                to: toParam,
                pageNumber: VoucherPage,
                pageSize: VoucherPageSize,
                ct: ct);

            StockInVouchers.Clear();
            if (res?.Data != null)
            {
                VoucherTotalCount = res.Data.TotalCount;
                VoucherTotalPages = Math.Max(1, (int)Math.Ceiling((double)VoucherTotalCount / VoucherPageSize));

                var items = res.Data.Items.AsEnumerable();
                if (!string.IsNullOrWhiteSpace(VoucherSearchKeyword))
                {
                    var kw = VoucherSearchKeyword.Trim();
                    items = items.Where(v =>
                        v.Id.ToString().Contains(kw, StringComparison.OrdinalIgnoreCase) ||
                        (!string.IsNullOrWhiteSpace(v.SupplierName) && v.SupplierName.Contains(kw, StringComparison.OrdinalIgnoreCase)) ||
                        (!string.IsNullOrWhiteSpace(v.Note) && v.Note.Contains(kw, StringComparison.OrdinalIgnoreCase)));
                }

                foreach (var v in items)
                {
                    StockInVouchers.Add(StockInVoucherItemModel.FromDto(v));
                }

                // Cập nhật số lượng phiếu nhập chờ duyệt trên thẻ KPI
                PendingVouchersCount = StockInVouchers.Count(v => v.Status == "Draft");
            }
        }
        catch (Exception ex)
        {
            ShowNotification($"Lỗi tải danh sách phiếu nhập: {ex.Message}", true);
        }
        finally
        {
            IsLoading = false;
            OnPropertyChanged(nameof(HasNoStockInVouchers));
            OnPropertyChanged(nameof(HasStockInVouchers));
        }
    }

    // ── Mở Modal Tạo Phiếu Nhập Kho Mới ──
    [RelayCommand]
    public void OpenCreateStockInModal()
    {
        EditingVoucherId = null;
        EditingVoucherStatus = "Draft";
        StockInNote = string.Empty;
        StockInBarcodeQuery = string.Empty;
        StockInLines.Clear();
        if (Suppliers.Count > 0 && SelectedSupplier == null)
        {
            SelectedSupplier = Suppliers[0];
        }

        UpdateStockInSummary();
        IsStockInModalOpen = true;
    }

    // ── Xem chi tiết phiếu nhập ──
    [RelayCommand]
    public async Task OpenViewStockInModalAsync(StockInVoucherItemModel? voucher)
    {
        if (voucher == null) return;
        IsLoading = true;
        try
        {
            var res = await _inventoryApi.GetStockInVoucherByIdAsync(voucher.Id);
            if (res?.Data != null)
            {
                var detail = res.Data;
                EditingVoucherId = detail.Id;
                EditingVoucherStatus = detail.Status;
                StockInNote = detail.Note ?? string.Empty;
                StockInBarcodeQuery = string.Empty;
                SelectedSupplier = Suppliers.FirstOrDefault(s => s.Id == detail.SupplierId) 
                    ?? new SupplierOptionModel { Id = detail.SupplierId, Name = detail.SupplierName };

                StockInLines.Clear();
                foreach (var item in detail.Items)
                {
                    var line = StockInLineItemModel.FromDto(item);
                    line.PropertyChanged += (s, e) => UpdateStockInSummary();
                    StockInLines.Add(line);
                }

                UpdateStockInSummary();
                IsStockInModalOpen = true;
            }
        }
        catch (Exception ex)
        {
            ShowNotification($"Lỗi tải chi tiết phiếu nhập: {ex.Message}", true);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public void CloseStockInModal()
    {
        IsStockInModalOpen = false;
    }

    // ── Quét mã vạch / Tìm SKU nhập nhanh ──
    [RelayCommand]
    public async Task ScanAndAddStockInLineAsync()
    {
        var query = StockInBarcodeQuery?.Trim();
        if (string.IsNullOrWhiteSpace(query)) return;

        try
        {
            // Kiểm tra xem dòng hàng này đã có trong danh sách chưa
            var existing = StockInLines.FirstOrDefault(l =>
                string.Equals(l.Barcode, query, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(l.SkuCode, query, StringComparison.OrdinalIgnoreCase));

            if (existing != null)
            {
                existing.Qty += 1;
                StockInBarcodeQuery = string.Empty;
                UpdateStockInSummary();
                return;
            }

            // Gọi API tra cứu SKU theo mã vạch
            var lookupRes = await _productApi.GetSkuByBarcodeAsync(query);
            if (lookupRes?.Data != null)
            {
                var sku = lookupRes.Data;
                var newLine = new StockInLineItemModel
                {
                    SkuId = sku.Id,
                    SkuCode = sku.SkuCode,
                    Barcode = sku.Barcode,
                    ProductName = sku.ProductName,
                    Qty = 1,
                    UnitPrice = 0m, // Barcode lookup không có CostPrice – operator phải nhập giá vốn
                    BatchNo = null,
                    ExpiryDate = null
                };

                newLine.PropertyChanged += (s, e) => UpdateStockInSummary();
                StockInLines.Add(newLine);
                StockInBarcodeQuery = string.Empty;
                UpdateStockInSummary();
                return;
            }

            // Nếu tra cứu barcode không có, tìm kiếm theo SKU code / ProductName trong POS catalog
            var searchRes = await _productApi.GetPosCatalogAsync(search: query, pageNumber: 1, pageSize: 5);
            if (searchRes?.Data?.Items != null && searchRes.Data.Items.Count > 0)
            {
                // PosCatalogItemResponse là flat record mỗi item là một SKU
                var catalogItem = searchRes.Data.Items.FirstOrDefault(s =>
                    string.Equals(s.Barcode, query, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(s.SkuCode, query, StringComparison.OrdinalIgnoreCase))
                    ?? searchRes.Data.Items[0];

                var newLine = new StockInLineItemModel
                {
                    SkuId = catalogItem.SkuId,
                    SkuCode = catalogItem.SkuCode,
                    Barcode = catalogItem.Barcode,
                    ProductName = catalogItem.ProductName,
                    Qty = 1,
                    UnitPrice = catalogItem.CostPrice > 0 ? catalogItem.CostPrice : 0m, // Operator phải nhập giá nếu không có CostPrice
                    BatchNo = null,
                    ExpiryDate = null
                };

                newLine.PropertyChanged += (s, e) => UpdateStockInSummary();
                StockInLines.Add(newLine);
                StockInBarcodeQuery = string.Empty;
                UpdateStockInSummary();
                return;
            }

            ShowNotification($"Không tìm thấy sản phẩm có mã '{query}'.", true);
        }
        catch (Exception ex)
        {
            ShowNotification($"Lỗi quét mã: {ex.Message}", true);
        }
    }

    [RelayCommand]
    public void RemoveStockInLine(StockInLineItemModel? line)
    {
        if (line == null) return;
        StockInLines.Remove(line);
        UpdateStockInSummary();
    }

    private void UpdateStockInSummary()
    {
        OnPropertyChanged(nameof(TotalStockInAmount));
        OnPropertyChanged(nameof(TotalStockInAmountFormatted));
        OnPropertyChanged(nameof(CanEditStockIn));
        OnPropertyChanged(nameof(CanCompleteEditingVoucher));
    }

    // ── Lưu Bản Nháp Phiếu Nhập Kho ──
    [RelayCommand]
    public async Task SaveDraftStockInVoucherAsync()
    {
        if (SelectedSupplier == null)
        {
            ShowNotification("Vui lòng chọn Nhà cung cấp.", true);
            return;
        }

        if (StockInLines.Count == 0)
        {
            ShowNotification("Phiếu nhập phải có ít nhất 1 mặt hàng.", true);
            return;
        }

        IsLoading = true;
        try
        {
            var items = StockInLines.Select(l => new StockInVoucherItemRequest(
                l.SkuId,
                l.Qty,
                l.UnitPrice,
                string.IsNullOrWhiteSpace(l.BatchNo) ? null : l.BatchNo.Trim(),
                l.ExpiryDate.HasValue ? DateOnly.FromDateTime(l.ExpiryDate.Value) : null
            )).ToList();

            var req = new CreateStockInVoucherRequest(SelectedSupplier.Id, items, StockInNote);
            var res = await _inventoryApi.CreateStockInVoucherAsync(req);

            if (res?.Data != null)
            {
                ShowNotification("Đã tạo phiếu nhập kho thành công (Bản nháp).");
                IsStockInModalOpen = false;
                await LoadStockInVouchersAsync();
            }
        }
        catch (Exception ex)
        {
            ShowNotification($"Lỗi lưu phiếu nhập: {ex.Message}", true);
        }
        finally
        {
            IsLoading = false;
        }
    }

    // ── Hoàn Thành Nhập Kho: Tăng tồn, ghi sổ cái ──
    [RelayCommand]
    public async Task CompleteStockInVoucherAsync(StockInVoucherItemModel? voucherParam = null)
    {
        var targetId = voucherParam?.Id ?? EditingVoucherId;
        if (!targetId.HasValue)
        {
            // Nếu đang trong modal tạo mới và chưa lưu, lưu trước rồi complete
            if (SelectedSupplier == null || StockInLines.Count == 0)
            {
                ShowNotification("Vui lòng chọn NCC và thêm mặt hàng trước khi nhập kho.", true);
                return;
            }

            IsLoading = true;
            try
            {
                var items = StockInLines.Select(l => new StockInVoucherItemRequest(
                    l.SkuId,
                    l.Qty,
                    l.UnitPrice,
                    string.IsNullOrWhiteSpace(l.BatchNo) ? null : l.BatchNo.Trim(),
                    l.ExpiryDate.HasValue ? DateOnly.FromDateTime(l.ExpiryDate.Value) : null
                )).ToList();

                var createReq = new CreateStockInVoucherRequest(SelectedSupplier.Id, items, StockInNote);
                var createRes = await _inventoryApi.CreateStockInVoucherAsync(createReq);
                if (createRes?.Data != null)
                {
                    targetId = createRes.Data.Id;
                    // Persist the new voucher state so a retry won't create a duplicate
                    EditingVoucherId = createRes.Data.Id;
                    EditingVoucherStatus = createRes.Data.Status ?? "Draft";
                }
            }
            catch (Exception ex)
            {
                ShowNotification($"Lỗi tạo phiếu nhập: {ex.Message}", true);
                IsLoading = false;
                return;
            }
        }

        if (!targetId.HasValue)
        {
            IsLoading = false;
            ShowNotification("Không xác định được mã phiếu nhập kho.", true);
            return;
        }

        IsLoading = true;
        try
        {
            var res = await _inventoryApi.CompleteStockInVoucherAsync(targetId.Value);
            if (res != null)
            {
                ShowNotification("Đã hoàn thành nhập kho! Số lượng tồn đã được cộng vào hệ thống.");
                IsStockInModalOpen = false;
                await LoadStockInVouchersAsync();
                await LoadStockEntriesAsync();
                await LoadStockAlertsAsync();
            }
        }
        catch (Exception ex)
        {
            ShowNotification($"Lỗi hoàn thành nhập kho: {ex.Message}", true);
        }
        finally
        {
            IsLoading = false;
        }
    }

    // ── Hủy phiếu nhập kho ──
    [RelayCommand]
    public async Task CancelStockInVoucherAsync(StockInVoucherItemModel? voucher)
    {
        var targetId = voucher?.Id ?? EditingVoucherId;
        if (!targetId.HasValue) return;

        IsLoading = true;
        try
        {
            var res = await _inventoryApi.CancelStockInVoucherAsync(targetId.Value);
            if (res != null)
            {
                ShowNotification("Đã hủy phiếu nhập kho.");
                IsStockInModalOpen = false;
                await LoadStockInVouchersAsync();
            }
        }
        catch (Exception ex)
        {
            ShowNotification($"Lỗi hủy phiếu: {ex.Message}", true);
        }
        finally
        {
            IsLoading = false;
        }
    }

    // ── Xem trước & Import phiếu nhập kho từ Excel của NCC ──
    [ObservableProperty]
    private bool _isStockInPreviewModalOpen = false;

    [ObservableProperty]
    private StockInImportPreviewResponse? _stockInPreview;

    [ObservableProperty]
    private string _pendingStockInFilePath = string.Empty;

    [ObservableProperty]
    private Guid _pendingStockInSupplierId = Guid.Empty;

    [ObservableProperty]
    private bool _isStockInPreviewLoading = false;

    public bool HasStockInPreviewErrors => StockInPreview != null && StockInPreview.Errors != null && StockInPreview.Errors.Count > 0;

    partial void OnStockInPreviewChanged(StockInImportPreviewResponse? value) => OnPropertyChanged(nameof(HasStockInPreviewErrors));

    /// <summary>
    /// Chọn file Excel và mở Modal Xem trước (Preview) kiểm tra tính hợp lệ của phiếu nhập kho
    /// </summary>
    [RelayCommand]
    public async Task ImportStockInExcelAsync()
    {
        if (Suppliers.Count == 0)
        {
            await LoadSuppliersAsync();
        }

        if (Suppliers.Count == 0)
        {
            ShowNotification("Hệ thống chưa có Nhà cung cấp. Vui lòng tạo Nhà cung cấp trước khi nhập kho.", true);
            return;
        }

        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "File Excel (*.xlsx;*.xls)|*.xlsx;*.xls|Tất cả tệp (*.*)|*.*",
            Title = "Chọn file Excel bảng kê nhập hàng từ Nhà Cung Cấp để kiểm tra và nhập"
        };

        if (dialog.ShowDialog() != true) return;

        var filePath = dialog.FileName;

        var supplier = (SelectedVoucherSupplierFilter != null && SelectedVoucherSupplierFilter.Id != Guid.Empty)
            ? SelectedVoucherSupplierFilter
            : (SelectedSupplier != null && SelectedSupplier.Id != Guid.Empty ? SelectedSupplier : Suppliers.FirstOrDefault(s => s.Id != Guid.Empty));
        if (supplier == null)
        {
            ShowNotification("Vui lòng chọn một Nhà cung cấp để gán phiếu nhập.", true);
            return;
        }

        PendingStockInFilePath = filePath;
        PendingStockInSupplierId = supplier.Id;
        IsStockInPreviewLoading = true;
        IsStockInPreviewModalOpen = true;
        StockInPreview = null;

        try
        {
            var res = await _inventoryApi.PreviewBulkImportStockInAsync(PendingStockInFilePath, PendingStockInSupplierId);
            if (res != null && res.Success && res.Data != null)
            {
                StockInPreview = res.Data;
            }
            else
            {
                var errMsg = res?.Error?.Message ?? "Không thể phân tích dữ liệu bảng kê từ file Excel.";
                ShowNotification(errMsg, true);
                IsStockInPreviewModalOpen = false;
            }
        }
        catch (Exception ex)
        {
            ShowNotification($"Lỗi kiểm tra file Excel: {ex.Message}", true);
            IsStockInPreviewModalOpen = false;
        }
        finally
        {
            IsStockInPreviewLoading = false;
        }
    }

    /// <summary>
    /// Đóng Modal Xem trước nhập kho
    /// </summary>
    [RelayCommand]
    public void CloseStockInPreviewModal()
    {
        IsStockInPreviewModalOpen = false;
        PendingStockInFilePath = string.Empty;
        PendingStockInSupplierId = Guid.Empty;
        StockInPreview = null;
    }

    /// <summary>
    /// Xác nhận tạo phiếu nhập kho chính thức sau khi xem trước
    /// </summary>
    [RelayCommand]
    public async Task ConfirmStockInBulkImportAsync()
    {
        if (string.IsNullOrWhiteSpace(PendingStockInFilePath) || !System.IO.File.Exists(PendingStockInFilePath))
        {
            ShowNotification("Không tìm thấy đường dẫn file đã chọn.", true);
            return;
        }

        if (PendingStockInSupplierId == Guid.Empty)
        {
            ShowNotification("Nhà cung cấp không hợp lệ.", true);
            return;
        }

        IsStockInPreviewModalOpen = false;
        IsLoading = true;
        ShowNotification("Đang xử lý tạo phiếu nhập kho và nạp tồn từ file Excel...");

        try
        {
            var res = await _inventoryApi.BulkImportStockInAsync(
                filePath: PendingStockInFilePath,
                supplierId: PendingStockInSupplierId,
                note: $"Nhập hàng Excel ({StockInPreview?.SupplierName ?? "NCC"})",
                autoComplete: true);

            if (res != null && res.Success && res.Data != null)
            {
                var data = res.Data;
                var summary = $"Nhập kho hoàn tất: {data.SuccessCount}/{data.TotalRows} dòng thành công. Tổng tiền: {data.TotalAmount:N0} đ (Mã phiếu: {data.VoucherCode}).";

                if (data.FailureCount > 0)
                {
                    ShowNotification($"{summary} Có {data.FailureCount} dòng lỗi.", true);
                }
                else
                {
                    ShowNotification(summary);
                }

                await LoadStockInVouchersAsync();
                await LoadStockEntriesAsync();
                await LoadStockAlertsAsync();
            }
            else
            {
                var errMsg = res?.Error?.Message ?? "Không thể nhập dữ liệu từ file Excel.";
                ShowNotification(errMsg, true);
            }
        }
        catch (Exception ex)
        {
            ShowNotification($"Lỗi import phiếu nhập kho: {ex.Message}", true);
        }
        finally
        {
            IsLoading = false;
            PendingStockInFilePath = string.Empty;
            PendingStockInSupplierId = Guid.Empty;
        }
    }
}
