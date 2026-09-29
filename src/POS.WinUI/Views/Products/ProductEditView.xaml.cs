using System.Windows;
using POS.WinUI.ViewModels.Products;

namespace POS.WinUI.Views.Products;

public partial class ProductEditView : Window
{
    public ProductEditView()
    {
        InitializeComponent();
    }

    protected override void OnPropertyChanged(System.Windows.DependencyPropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        // Refresh barcode image whenever SelectedSku.Barcode changes
        if (DataContext is ProductEditViewModel vm)
        {
            vm.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(vm.SelectedSku))
                    RefreshBarcodePreview(vm);
            };
        }
    }

    protected override void OnContentRendered(EventArgs e)
    {
        base.OnContentRendered(e);
        if (DataContext is ProductEditViewModel vm)
        {
            vm.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName is nameof(vm.SelectedSku))
                    RefreshBarcodePreview(vm);
            };
        }
    }

    private void RefreshBarcodePreview(ProductEditViewModel vm)
    {
        Dispatcher.InvokeAsync(() =>
        {
            if (BarcodePreviewImage != null)
                BarcodePreviewImage.Source = vm.SelectedSku is { Barcode.Length: > 0 }
                    ? ProductEditViewModel.GenerateBarcodeImage(vm.SelectedSku.Barcode)
                    : null;
        });
    }
}
