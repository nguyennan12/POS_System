using System.Windows.Controls;
using System.Windows.Input;
using POS.WinUI.ViewModels.SmartStore;

namespace POS.WinUI.Views.SmartStore;

public partial class ProductPlacementModal : UserControl
{
    public ProductPlacementModal()
    {
        InitializeComponent();
    }

    private void OnBarcodeInputKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && DataContext is ProductPlacementViewModel vm)
        {
            if (vm.LookupBarcodeCommand.CanExecute(null))
            {
                vm.LookupBarcodeCommand.Execute(null);
            }
        }
    }
}
