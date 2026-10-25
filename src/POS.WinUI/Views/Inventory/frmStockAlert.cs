using System;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using POS.WinUI.ViewModels.Inventory;

namespace POS.WinUI.Views.Inventory;

/// <summary>
/// Màn hình cảnh báo tồn kho và hàng cận date (frmStockAlert)
/// Đáp ứng yêu cầu task T33. Hiển thị màu sắc cảnh báo trực quan cho hàng sắp hết hoặc cận date.
/// </summary>
public partial class frmStockAlert : Window
{
    public InventoryViewModel ViewModel { get; }

    public frmStockAlert() : this(App.Services?.GetService<InventoryViewModel>()!)
    {
    }

    public frmStockAlert(InventoryViewModel viewModel)
    {
        ViewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        DataContext = ViewModel;

        Title = "Cảnh báo tồn kho & Hạn dùng - POS System";
        Width = 1180;
        Height = 800;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        Background = System.Windows.Media.Brushes.White;

        // Chọn tab cảnh báo tồn kho (Tab 2), bật bộ lọc chỉ xem cảnh báo
        ViewModel.SelectedTabIndex = 2;
        ViewModel.ShowOnlyAlerts = true;

        // Nhúng InventoryView làm Content của Window
        Content = new InventoryView { DataContext = ViewModel };
    }
}
