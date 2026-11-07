using System;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using POS.WinUI.ViewModels.Inventory;

namespace POS.WinUI.Views.Inventory;

/// <summary>
/// Màn hình kiểm kê kho thực tế (frmStockTake)
/// Đáp ứng yêu cầu task T33. Tự động tính chênh lệch thực tế vs hệ thống, hỗ trợ Quản lý phê duyệt cân bằng kho.
/// </summary>
public partial class frmStockTake : Window
{
    public InventoryViewModel ViewModel { get; }

    public frmStockTake() : this(App.Services?.GetService<InventoryViewModel>()!)
    {
    }

    public frmStockTake(InventoryViewModel viewModel)
    {
        ViewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        DataContext = ViewModel;

        Title = "Kiểm kê kho hàng hóa - POS System";
        Width = 1040;
        Height = 780;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        Background = System.Windows.Media.Brushes.White;

        // Kích hoạt mở modal tạo đợt kiểm kê mới (chỉ reset state cục bộ, không tạo Draft ngay)
        ViewModel.OpenCreateStockTakeModalAsync();

        // Nhúng giao diện StockTakeModal làm Content của Window
        Content = new StockTakeModal { DataContext = ViewModel };
    }
}
