using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using POS.Contracts.V1.Products;

namespace POS.WinUI.ViewModels.Products;

public partial class ProductsViewModel
{
    // ── Trạng thái Modal Kết quả Nhập Excel ──
    [ObservableProperty]
    private bool _isImportResultModalOpen = false;

    [ObservableProperty]
    private BulkImportResultResponse? _lastImportResult;

    [ObservableProperty]
    private string _importSummaryText = string.Empty;

    public bool HasImportErrors => LastImportResult != null && LastImportResult.Errors != null && LastImportResult.Errors.Count > 0;

    partial void OnLastImportResultChanged(BulkImportResultResponse? value) => OnPropertyChanged(nameof(HasImportErrors));

    /// <summary>
    /// [P05] Chọn file Excel và gọi API Bulk Import để nhập hàng loạt sản phẩm & SKU
    /// </summary>
    [RelayCommand]
    public async Task ImportExcelAsync()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "File Excel (*.xlsx;*.xls)|*.xlsx;*.xls|Tất cả tệp (*.*)|*.*",
            Title = "Chọn file Excel danh sách sản phẩm để nhập hàng loạt"
        };

        if (dialog.ShowDialog() != true) return;

        var filePath = dialog.FileName;
        IsLoading = true;
        ShowStatus("Đang xử lý tải lên và nhập dữ liệu từ file Excel...");

        try
        {
            var res = await _productApi.BulkImportProductsAsync(filePath);
            if (res != null && res.Success && res.Data != null)
            {
                LastImportResult = res.Data;
                ImportSummaryText = $"Xử lý hoàn tất: {res.Data.SuccessCount}/{res.Data.TotalRows} sản phẩm thành công.";

                if (res.Data.FailureCount > 0)
                {
                    // Có lỗi một số dòng -> Mở modal chi tiết để người dùng xem và khắc phục
                    IsImportResultModalOpen = true;
                    ShowStatus($"Nhập hoàn tất với {res.Data.FailureCount} lỗi. Vui lòng xem bảng chi tiết.", isError: true);
                }
                else
                {
                    ShowStatus($"Nhập thành công toàn bộ {res.Data.SuccessCount} sản phẩm từ Excel!");
                }

                // Tự động tải lại danh sách sản phẩm và danh mục mới nhất
                CurrentPage = 1;
                await LoadCategoriesAsync();
                await LoadProductsAsync();
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
        }
    }

    [RelayCommand]
    public void CloseImportResultModal()
    {
        IsImportResultModalOpen = false;
    }
}
