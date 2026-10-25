using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using POS.Contracts.V1.Reports;
using POS.WinUI.Core.ApiClients;
using POS.WinUI.Core.Models;
using POS.WinUI.Core.Services;
using POS.WinUI.ViewModels.Common;
using Wpf.Ui.Controls;

namespace POS.WinUI.ViewModels.Reports;

/// <summary>
/// ViewModel cho phân hệ Báo cáo doanh thu đa chiều & Báo cáo tồn kho (T57)
/// </summary>
public sealed partial class ReportsViewModel : ManagementTabViewModelBase
{
    private readonly ReportApiClient _reportApiClient;
    private readonly StoreApiClient _storeApiClient;

    public override string TabId => "Reports";
    public override string Title => "Báo cáo & Thống kê";
    public override string Subtitle => "Báo cáo doanh thu đa chiều, lợi nhuận, mặt hàng bán chạy và xuất file Excel";
    public override SymbolRegular Icon => SymbolRegular.DataPie24;

    // ── Tab Type (0: Revenue, 1: Inventory) ─────────────────────────────────
    [ObservableProperty]
    private int _selectedReportType = 0;

    public bool IsRevenueTab
    {
        get => SelectedReportType == 0;
        set
        {
            if (value && SelectedReportType != 0)
            {
                SelectedReportType = 0;
                OnPropertyChanged(nameof(IsRevenueTab));
                OnPropertyChanged(nameof(IsInventoryTab));
                _ = LoadCurrentReportAsync();
            }
        }
    }

    public bool IsInventoryTab
    {
        get => SelectedReportType == 1;
        set
        {
            if (value && SelectedReportType != 1)
            {
                SelectedReportType = 1;
                OnPropertyChanged(nameof(IsRevenueTab));
                OnPropertyChanged(nameof(IsInventoryTab));
                _ = LoadCurrentReportAsync();
            }
        }
    }

    // ── Date Filters ────────────────────────────────────────────────────────
    [ObservableProperty]
    private DateTime _fromDate = DateTime.Today.AddDays(-30);

    [ObservableProperty]
    private DateTime _toDate = DateTime.Today;

    // ── Store Filters ───────────────────────────────────────────────────────
    [ObservableProperty]
    private ObservableCollection<StoreItem> _stores = new();

    [ObservableProperty]
    private StoreItem? _selectedStore;

    // ── Revenue Report Metrics ──────────────────────────────────────────────
    [ObservableProperty]
    private decimal _totalRevenue;

    [ObservableProperty]
    private decimal _totalDiscount;

    [ObservableProperty]
    private decimal _totalTax;

    [ObservableProperty]
    private decimal _netRevenue;

    [ObservableProperty]
    private int _totalOrders;

    [ObservableProperty]
    private ObservableCollection<RevenueByPeriodItem> _periods = new();

    public bool HasPeriods => Periods.Count > 0;

    public string TotalRevenueFormatted => TotalRevenue.ToString("N0") + " đ";
    public string TotalDiscountFormatted => TotalDiscount.ToString("N0") + " đ";
    public string TotalTaxFormatted => TotalTax.ToString("N0") + " đ";
    public string NetRevenueFormatted => NetRevenue.ToString("N0") + " đ";

    // ── Inventory Report Metrics ────────────────────────────────────────────
    [ObservableProperty]
    private int _totalSkus;

    [ObservableProperty]
    private decimal _totalStockValue;

    [ObservableProperty]
    private int _outOfStockCount;

    [ObservableProperty]
    private int _lowStockCount;

    [ObservableProperty]
    private ObservableCollection<InventoryReportItem> _inventoryItems = new();

    public bool HasInventoryItems => InventoryItems.Count > 0;
    public string TotalStockValueFormatted => TotalStockValue.ToString("N0") + " đ";

    // ── Export State ────────────────────────────────────────────────────────
    [ObservableProperty]
    private bool _isExporting;

    public ReportsViewModel(
        ReportApiClient reportApiClient,
        StoreApiClient storeApiClient,
        SessionService sessionService) : base(sessionService)
    {
        _reportApiClient = reportApiClient;
        _storeApiClient = storeApiClient;

        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        await LoadStoresAsync();
        await LoadCurrentReportAsync();
    }

    private async Task LoadStoresAsync()
    {
        try
        {
            if (IsOwner)
            {
                var response = await _storeApiClient.GetAllAsync();
                if (response?.Success == true && response.Data != null)
                {
                    Stores.Clear();
                    Stores.Add(new StoreItem { Id = Guid.Empty, Name = "Tất cả chi nhánh" });
                    foreach (var s in response.Data)
                    {
                        Stores.Add(new StoreItem { Id = s.Id, Name = s.Name });
                    }
                    SelectedStore = Stores.FirstOrDefault();
                }
            }
        }
        catch
        {
            // Bỏ qua lỗi kết nối store
        }
    }

    [RelayCommand]
    public async Task SwitchTabAsync(object? parameter)
    {
        int tabIndex = 0;
        if (parameter is int i)
        {
            tabIndex = i;
        }
        else if (parameter is string s && int.TryParse(s, out var parsed))
        {
            tabIndex = parsed;
        }

        SelectedReportType = tabIndex;
        OnPropertyChanged(nameof(IsRevenueTab));
        OnPropertyChanged(nameof(IsInventoryTab));
        await LoadCurrentReportAsync();
    }

    [RelayCommand]
    public async Task SetPresetPeriodAsync(string preset)
    {
        var today = DateTime.Today;
        switch (preset?.ToLower())
        {
            case "today":
                FromDate = today;
                ToDate = today;
                break;
            case "7days":
                FromDate = today.AddDays(-6);
                ToDate = today;
                break;
            case "30days":
                FromDate = today.AddDays(-29);
                ToDate = today;
                break;
            case "thismonth":
                FromDate = new DateTime(today.Year, today.Month, 1);
                ToDate = today;
                break;
        }

        await LoadCurrentReportAsync();
    }

    [RelayCommand]
    public async Task LoadCurrentReportAsync()
    {
        if (IsLoading) return;
        IsLoading = true;

        try
        {
            Guid? storeId = null;
            if (SelectedStore != null && SelectedStore.Id != Guid.Empty)
            {
                storeId = SelectedStore.Id;
            }
            else if (!IsOwner && !string.IsNullOrWhiteSpace(CurrentStoreId) && Guid.TryParse(CurrentStoreId, out var parsedStoreId))
            {
                storeId = parsedStoreId;
            }

            if (IsRevenueTab)
            {
                var request = new RevenueReportFilterRequest(
                    From: new DateTimeOffset(FromDate.Date, TimeSpan.Zero),
                    To: new DateTimeOffset(ToDate.Date.AddDays(1).AddTicks(-1), TimeSpan.Zero),
                    StoreId: storeId);

                var response = await _reportApiClient.GetRevenueReportAsync(request);
                if (response?.Success == true && response.Data != null)
                {
                    var data = response.Data;
                    TotalRevenue = data.TotalRevenue;
                    TotalDiscount = data.TotalDiscount;
                    TotalTax = data.TotalTax;
                    NetRevenue = data.NetRevenue;
                    TotalOrders = data.TotalOrders;

                    OnPropertyChanged(nameof(TotalRevenueFormatted));
                    OnPropertyChanged(nameof(TotalDiscountFormatted));
                    OnPropertyChanged(nameof(TotalTaxFormatted));
                    OnPropertyChanged(nameof(NetRevenueFormatted));

                    Periods.Clear();
                    foreach (var p in data.Periods)
                    {
                        Periods.Add(p);
                    }
                    OnPropertyChanged(nameof(HasPeriods));
                }
                else
                {
                    ShowError(response?.Message ?? "Không thể tải báo cáo doanh thu.");
                }
            }
            else
            {
                var request = new InventoryReportFilterRequest(StoreId: storeId);
                var response = await _reportApiClient.GetInventoryReportAsync(request);
                if (response?.Success == true && response.Data != null)
                {
                    var data = response.Data;
                    TotalSkus = data.TotalSkus;
                    TotalStockValue = data.TotalStockValue;
                    OutOfStockCount = data.OutOfStockCount;
                    LowStockCount = data.LowStockCount;

                    OnPropertyChanged(nameof(TotalStockValueFormatted));

                    InventoryItems.Clear();
                    foreach (var item in data.Items)
                    {
                        InventoryItems.Add(item);
                    }
                    OnPropertyChanged(nameof(HasInventoryItems));
                }
                else
                {
                    ShowError(response?.Message ?? "Không thể tải báo cáo tồn kho.");
                }
            }
        }
        catch (Exception ex)
        {
            ShowError($"Lỗi khi tải báo cáo: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task ExportExcelAsync()
    {
        if (IsExporting) return;
        IsExporting = true;

        try
        {
            Guid? storeId = null;
            if (SelectedStore != null && SelectedStore.Id != Guid.Empty)
            {
                storeId = SelectedStore.Id;
            }
            else if (!IsOwner && !string.IsNullOrWhiteSpace(CurrentStoreId) && Guid.TryParse(CurrentStoreId, out var parsedStoreId))
            {
                storeId = parsedStoreId;
            }

            var type = IsRevenueTab ? "revenue" : "inventory";
            var parameters = new Dictionary<string, string>();
            if (storeId.HasValue)
            {
                parameters["storeId"] = storeId.Value.ToString();
            }

            if (IsRevenueTab)
            {
                parameters["from"] = FromDate.ToString("yyyy-MM-dd");
                parameters["to"] = ToDate.ToString("yyyy-MM-dd");
            }

            var exportRequest = new ExportReportRequest(
                Type: type,
                Format: "xlsx",
                Params: parameters);

            ShowInfo("Đang tạo lệnh xuất báo cáo Excel...");
            var jobResponse = await _reportApiClient.ExportReportAsync(exportRequest);

            if (jobResponse?.Success == true && jobResponse.Data != null)
            {
                var jobId = jobResponse.Data.JobId;
                byte[]? fileBytes = null;

                // Poll for completion (up to 15 seconds)
                for (int i = 0; i < 15; i++)
                {
                    var statusResponse = await _reportApiClient.GetExportJobStatusAsync(jobId);
                    if (statusResponse?.Success == true && statusResponse.Data != null)
                    {
                        var status = statusResponse.Data.Status?.ToLower();
                        if (status == "completed")
                        {
                            fileBytes = await _reportApiClient.DownloadExportReportAsync(jobId);
                            break;
                        }
                        if (status == "failed")
                        {
                            ShowError($"Lỗi xuất file: {statusResponse.Data.ErrorMessage ?? "Không xác định"}");
                            return;
                        }
                    }
                    await Task.Delay(1000);
                }

                if (fileBytes != null && fileBytes.Length > 0)
                {
                    var downloadsPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                    downloadsPath = Path.Combine(downloadsPath, "Downloads");
                    if (!Directory.Exists(downloadsPath))
                    {
                        downloadsPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                    }

                    var fileName = $"BaoCao_{type}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                    var fullPath = Path.Combine(downloadsPath, fileName);
                    await File.WriteAllBytesAsync(fullPath, fileBytes);

                    ShowSuccess($"Xuất file Excel thành công! Lưu tại:\n{fullPath}");

                    try
                    {
                        Process.Start(new ProcessStartInfo(fullPath) { UseShellExecute = true });
                    }
                    catch
                    {
                        // File đã lưu, bỏ qua nếu hệ thống không tự mở file
                    }
                }
                else
                {
                    ShowError("Hết thời gian chờ tạo file Excel. Vui lòng thử lại.");
                }
            }
            else
            {
                ShowError(jobResponse?.Message ?? "Không thể yêu cầu xuất file báo cáo.");
            }
        }
        catch (Exception ex)
        {
            ShowError($"Lỗi khi xuất file Excel: {ex.Message}");
        }
        finally
        {
            IsExporting = false;
        }
    }
}
