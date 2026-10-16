using System.ComponentModel;
using System.Windows;
using POS.WinUI.ViewModels.Products;

namespace POS.WinUI.Views.Products;

public partial class ProductEditModalWindow : Window
{
    private readonly ProductsViewModel _viewModel;

    public ProductEditModalWindow(ProductsViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;
        EditModalContent.DataContext = _viewModel;

        if (Application.Current?.MainWindow != null)
        {
            Owner = Application.Current.MainWindow;
            Width = Owner.ActualWidth > 0 ? Owner.ActualWidth : 1200;
            Height = Owner.ActualHeight > 0 ? Owner.ActualHeight : 800;
            Left = Owner.Left;
            Top = Owner.Top;
        }

        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        Closed += (s, e) => _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ProductsViewModel.IsProductEditModalOpen))
        {
            if (!_viewModel.IsProductEditModalOpen)
            {
                Close();
            }
        }
    }
}
