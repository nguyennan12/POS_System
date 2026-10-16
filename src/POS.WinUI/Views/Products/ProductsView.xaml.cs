using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using POS.WinUI.ViewModels.Products;

namespace POS.WinUI.Views.Products;

public partial class ProductsView : UserControl
{
    private ProductEditModalWindow? _modalWindow;

    public ProductsView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is ProductsViewModel oldVm)
        {
            oldVm.PropertyChanged -= OnViewModelPropertyChanged;
        }

        if (e.NewValue is ProductsViewModel newVm)
        {
            newVm.PropertyChanged += OnViewModelPropertyChanged;
        }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ProductsViewModel.IsProductEditModalOpen))
        {
            if (DataContext is ProductsViewModel vm)
            {
                if (vm.IsProductEditModalOpen)
                {
                    if (_modalWindow == null || !_modalWindow.IsLoaded)
                    {
                        _modalWindow = new ProductEditModalWindow(vm);
                        _modalWindow.Closed += (s, args) =>
                        {
                            _modalWindow = null;
                            if (vm.IsProductEditModalOpen)
                            {
                                vm.CloseProductEditModal();
                            }
                        };
                        _modalWindow.ShowDialog();
                    }
                }
                else
                {
                    _modalWindow?.Close();
                    _modalWindow = null;
                }
            }
        }
    }
}

