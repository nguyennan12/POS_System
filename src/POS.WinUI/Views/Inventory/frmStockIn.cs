using System;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using POS.WinUI.ViewModels.Inventory;

namespace POS.WinUI.Views.Inventory;

/// <summary>
/// Màn hình nhập kho hàng hóa từ Nhà cung cấp (frmStockIn)
/// Đáp ứng yêu cầu task T33. Hỗ trợ quét mã vạch nhập nhanh và tạo số lô, hạn sử dụng.
/// </summary>
public partial class frmStockIn : Window
{
    public InventoryViewModel ViewModel { get; }

    public frmStockIn() : this(App.Services?.GetService<InventoryViewModel>()!)
    {
    }

    public frmStockIn(InventoryViewModel viewModel)
    {
        ViewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        DataContext = ViewModel;

        Title = "Nhập kho hàng hóa - POS System";
        Width = 1040;
        Height = 780;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        Background = System.Windows.Media.Brushes.White;

        // Kích hoạt mở modal tạo phiếu nhập
        ViewModel.OpenCreateStockInModal();

        // Nhúng giao diện StockInModal làm Content chính của Window
        Content = new StockInModal { DataContext = ViewModel };
    }
}
