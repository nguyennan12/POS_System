using System;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using POS.WinUI.ViewModels.Products;

namespace POS.WinUI.Views.Products;

/// <summary>
/// Màn hình danh sách sản phẩm & danh mục (frmProductList)
/// Tương thích với cấu trúc task T27. Có thể mở dạng Window độc lập hoặc nhúng trong ManagementMainView.
/// </summary>
public partial class frmProductList : Window
{
    public ProductsViewModel ViewModel { get; }

    public frmProductList() : this(App.Services?.GetService<ProductsViewModel>()!)
    {
    }

    public frmProductList(ProductsViewModel viewModel)
    {
        ViewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        DataContext = ViewModel;

        Title = "Danh sách hàng hóa & Danh mục - POS System";
        Width = 1240;
        Height = 820;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        Background = System.Windows.Media.Brushes.White;

        // Nhúng ProductsView làm Content
        Content = new ProductsView { DataContext = ViewModel };
    }
}
