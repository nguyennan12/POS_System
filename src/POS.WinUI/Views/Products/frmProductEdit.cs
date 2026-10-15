using System;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using POS.WinUI.ViewModels.Products;

namespace POS.WinUI.Views.Products;

/// <summary>
/// Form chi tiết sản phẩm & biến thể SKU (frmProductEdit)
/// Tương thích với cấu trúc task T27. Hỗ trợ tạo biến thể màu/size động và in tem mã vạch.
/// </summary>
public partial class frmProductEdit : Window
{
    public ProductsViewModel ViewModel { get; }

    public frmProductEdit(ProductItemModel? productToEdit = null) 
        : this(App.Services?.GetService<ProductsViewModel>()!, productToEdit)
    {
    }

    public frmProductEdit(ProductsViewModel viewModel, ProductItemModel? productToEdit = null)
    {
        ViewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        DataContext = ViewModel;

        Title = productToEdit == null ? "Thêm mới sản phẩm & Biến thể SKU" : $"Chỉnh sửa sản phẩm: {productToEdit.Name}";
        Width = 920;
        Height = 780;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        Background = System.Windows.Media.Brushes.White;

        if (productToEdit == null)
        {
            ViewModel.OpenCreateProductModal();
        }
        else
        {
            _ = ViewModel.OpenEditProductModalAsync(productToEdit);
        }

        // Tạo container hiển thị ProductEditModal
        Content = new ProductEditModal { DataContext = ViewModel };
    }
}
