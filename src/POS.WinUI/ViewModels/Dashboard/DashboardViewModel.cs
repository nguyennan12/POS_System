using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using POS.Contracts.V1.Reports;
using POS.WinUI.Core.ApiClients;
using POS.WinUI.Core.Models;
using POS.WinUI.Core.Services;
using POS.WinUI.ViewModels.Common;
using Wpf.Ui.Controls;

namespace POS.WinUI.ViewModels.Dashboard;

public class DashboardChartBarItem
{
    public string DateLabel { get; set; } = string.Empty;
    public decimal Revenue { get; set; }
    public int OrderCount { get; set; }
    public double BarHeight { get; set; }
    public string FormattedRevenue => Revenue.ToString("N0") + " đ";
    public string TooltipText => $"{DateLabel}: {FormattedRevenue} ({OrderCount} đơn)";
}

/// <summary>
/// ViewModel chính cho phân hệ Tổng quan kinh doanh thời gian thực (Executive Dashboard)
/// </summary>
public sealed partial class DashboardViewModel : ManagementTabViewModelBase
{
    private readonly ReportApiClient _reportApiClient;
    private readonly StoreApiClient _storeApiClient;

    public override string TabId => "Dashboard";
    public override string Title => "Tổng quan kinh doanh";
    public override string Subtitle => "Theo dõi doanh thu, số lượng đơn hàng và hiệu suất bán lẻ thời gian thực";
    public override SymbolRegular Icon => SymbolRegular.Grid24;

    [ObservableProperty]
    private int _selectedDays = 7;

    [ObservableProperty]
    private StoreItem? _selectedStore;

    [ObservableProperty]
    private ObservableCollection<StoreItem> _stores = new();

    // ── KPI Metrics ────────────────────────────────────────────────────────
    [ObservableProperty]
    private decimal _todayRevenue;

    [ObservableProperty]
    private int _todayOrders;

    [ObservableProperty]
    private decimal _averageOrderValue;

    [ObservableProperty]
    private decimal _revenueGrowthPercentage;

    [ObservableProperty]
    private int _lowStockCount;

    [ObservableProperty]
    private int _expiringSoonCount;

    [ObservableProperty]
    private int _pendingStockTakeCount;

    public bool IsGrowthPositive => RevenueGrowthPercentage >= 0;
    public string TodayRevenueFormatted => TodayRevenue.ToString("N0") + " đ";
    public string AverageOrderValueFormatted => AverageOrderValue.ToString("N0") + " đ";
    public string GrowthPercentageFormatted => (RevenueGrowthPercentage >= 0 ? "+" : "") + RevenueGrowthPercentage.ToString("F1") + "%";

    // ── Collections ────────────────────────────────────────────────────────
    [ObservableProperty]
    private ObservableCollection<DashboardChartBarItem> _chartBars = new();

    [ObservableProperty]
    private ObservableCollection<TopSellingProductItem> _topSellingProducts = new();

    public bool HasTopSellingProducts => TopSellingProducts.Count > 0;

    public DashboardViewModel(
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
        await LoadDashboardAsync();
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
            // Bỏ qua lỗi load store danh sách nếu đang offline
        }
    }

    [RelayCommand]
    public async Task LoadDashboardAsync()
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

            var response = await _reportApiClient.GetDashboardSummaryAsync(storeId, SelectedDays);
            if (response?.Success == true && response.Data != null)
            {
                var data = response.Data;
                TodayRevenue = data.TodayRevenue;
                TodayOrders = data.TodayOrders;
                AverageOrderValue = data.AverageOrderValue;
                RevenueGrowthPercentage = data.RevenueGrowthPercentage;

                LowStockCount = data.Alerts.LowStockItemCount;
                ExpiringSoonCount = data.Alerts.ExpiringSoonBatchCount;
                PendingStockTakeCount = data.Alerts.PendingStockTakeCount;

                // Re-notify computed getters
                OnPropertyChanged(nameof(IsGrowthPositive));
                OnPropertyChanged(nameof(TodayRevenueFormatted));
                OnPropertyChanged(nameof(AverageOrderValueFormatted));
                OnPropertyChanged(nameof(GrowthPercentageFormatted));

                // Process chart bars
                ChartBars.Clear();
                if (data.RevenueChart.Any())
                {
                    var maxRevenue = Math.Max(1m, data.RevenueChart.Max(x => x.Revenue));
                    foreach (var item in data.RevenueChart)
                    {
                        var normalizedHeight = item.Revenue == 0 ? 6.0 : Math.Max(12.0, (double)(item.Revenue / maxRevenue) * 160.0);
                        ChartBars.Add(new DashboardChartBarItem
                        {
                            DateLabel = item.Date.ToString("dd/MM"),
                            Revenue = item.Revenue,
                            OrderCount = item.OrderCount,
                            BarHeight = normalizedHeight
                        });
                    }
                }

                // Process top selling products
                TopSellingProducts.Clear();
                foreach (var p in data.TopSellingProducts)
                {
                    TopSellingProducts.Add(p);
                }
                OnPropertyChanged(nameof(HasTopSellingProducts));
            }
            else
            {
                ShowError(response?.Message ?? "Không thể tải số liệu tổng quan Dashboard.");
            }
        }
        catch (Exception ex)
        {
            ShowError($"Lỗi khi nạp dữ liệu Dashboard: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task SetDaysFilterAsync(string days)
    {
        if (int.TryParse(days, out var d))
        {
            SelectedDays = d;
            await LoadDashboardAsync();
        }
    }

    [RelayCommand]
    public async Task OnStoreChangedAsync()
    {
        await LoadDashboardAsync();
    }
}
