using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using POS.Contracts.V1.Products;

namespace POS.WinUI.ViewModels.Products;

public partial class ProductsViewModel
{
    // ── Trạng thái Modal Xem Trước (Preview) Nhập Excel ──
    [ObservableProperty]
    private bool _isImportPreviewModalOpen = false;

    [ObservableProperty]
    private ProductImportPreviewResponse? _importPreview;

    [ObservableProperty]
    private string _pendingImportFilePath = string.Empty;

    [ObservableProperty]
    private bool _isPreviewLoading = false;

    public bool HasPreviewErrors => ImportPreview != null && ImportPreview.Errors != null && ImportPreview.Errors.Count > 0;

    // ── Cấu hình mặc định khi Import ──
    [ObservableProperty]
    private decimal? _defaultInitialStock;

    [ObservableProperty]
    private decimal? _defaultMinStock;

    [ObservableProperty]
    private bool _autoCreateCategories = true;

    // ── Trạng thái Modal Kết quả Nhập Excel ──
    [ObservableProperty]
    private bool _isImportResultModalOpen = false;

    [ObservableProperty]
    private BulkImportResultResponse? _lastImportResult;

    [ObservableProperty]
    private string _importSummaryText = string.Empty;

    public bool HasImportErrors => LastImportResult != null && LastImportResult.Errors != null && LastImportResult.Errors.Count > 0;

    partial void OnLastImportResultChanged(BulkImportResultResponse? value) => OnPropertyChanged(nameof(HasImportErrors));
    partial void OnImportPreviewChanged(ProductImportPreviewResponse? value) => OnPropertyChanged(nameof(HasPreviewErrors));

    /// <summary>
    /// [P05] Chọn file Excel và mở Modal Xem trước (Preview) kiểm tra tính hợp lệ trước khi nhập
    /// </summary>
    [RelayCommand]
    public async Task ImportExcelAsync()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "File Excel (*.xlsx;*.xls)|*.xlsx;*.xls|Tất cả tệp (*.*)|*.*",
            Title = "Chọn file Excel danh sách sản phẩm để kiểm tra và nhập hàng loạt"
        };

        if (dialog.ShowDialog() != true) return;

        PendingImportFilePath = dialog.FileName;
        IsPreviewLoading = true;
        IsImportPreviewModalOpen = true;
        ImportPreview = null;

        try
        {
            var res = await _productApi.PreviewBulkImportProductsAsync(PendingImportFilePath);
            if (res != null && res.Success && res.Data != null)
            {
                ImportPreview = res.Data;
            }
            else
            {
                var errMsg = res?.Error?.Message ?? "Không thể phân tích dữ liệu xem trước từ file Excel.";
                ShowStatus(errMsg, isError: true);
                IsImportPreviewModalOpen = false;
            }
        }
        catch (Exception ex)
        {
            ShowStatus($"Lỗi kiểm tra file Excel: {ex.Message}", isError: true);
            IsImportPreviewModalOpen = false;
        }
        finally
        {
            IsPreviewLoading = false;
        }
    }

    /// <summary>
    /// Đóng Modal Xem trước
    /// </summary>
    [RelayCommand]
    public void CloseImportPreviewModal()
    {
        IsImportPreviewModalOpen = false;
        PendingImportFilePath = string.Empty;
        ImportPreview = null;
    }

    /// <summary>
    /// Xác nhận thực hiện Import sau khi người dùng đã xem trước và đồng ý
    /// </summary>
    [RelayCommand]
    public async Task ConfirmBulkImportAsync()
    {
        if (string.IsNullOrWhiteSpace(PendingImportFilePath) || !System.IO.File.Exists(PendingImportFilePath))
        {
            ShowStatus("Không tìm thấy đường dẫn file đã chọn.", isError: true);
            return;
        }

        IsImportPreviewModalOpen = false;
        IsLoading = true;
        ShowStatus("Đang xử lý tải lên và nhập dữ liệu từ file Excel...", isError: false, showToast: false);

        try
        {
            var res = await _productApi.BulkImportProductsAsync(
                PendingImportFilePath,
                defaultInitialStock: DefaultInitialStock > 0 ? DefaultInitialStock : null,
                defaultMinStock: DefaultMinStock > 0 ? DefaultMinStock : null,
                autoCreateCategories: AutoCreateCategories);

            if (res != null && res.Success && res.Data != null)
            {
                LastImportResult = res.Data;
                ImportSummaryText = $"Xử lý hoàn tất: {res.Data.SuccessCount}/{res.Data.TotalRows} sản phẩm thành công.";

                if (res.Data.FailureCount > 0)
                {
                    IsImportResultModalOpen = true;
                    ShowStatus($"Nhập hoàn tất với {res.Data.FailureCount} dòng lỗi. Vui lòng kiểm tra báo cáo chi tiết.", isError: true);
                }
                else
                {
                    ShowStatus($"Nhập thành công toàn bộ {res.Data.SuccessCount} sản phẩm từ Excel!");
                }

                // Tự động tải lại danh sách sản phẩm, danh mục và tồn kho mới nhất
                _queryCache.InvalidateByPrefix("products");
                _queryCache.InvalidateByPrefix("categories");
                _queryCache.InvalidateByPrefix("kpi");
                _queryCache.InvalidateByPrefix("pos-catalog");
                _queryCache.InvalidateByPrefix("stock");
                _queryCache.InvalidateByPrefix("inventory");

                CurrentPage = 1;
                await LoadCategoriesAsync();
                await LoadProductsAsync();
                await LoadKpiSummaryAsync();
            }
            else
            {
                var errMsg = res?.Error?.Message ?? "Không thể nhập dữ liệu từ file Excel. Vui lòng kiểm tra cấu trúc file.";
                ShowStatus(errMsg, isError: true);
            }
        }
        catch (Exception ex)
        {
            ShowStatus($"Lỗi khi import file Excel: {ex.Message}", isError: true);
        }
        finally
        {
            IsLoading = false;
            PendingImportFilePath = string.Empty;
        }
    }

    [RelayCommand]
    public void CloseImportResultModal()
    {
        IsImportResultModalOpen = false;
    }
}
