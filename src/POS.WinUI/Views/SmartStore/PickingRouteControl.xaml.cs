using System.Windows.Controls;
using System.Windows.Input;
using POS.WinUI.ViewModels.SmartStore;

namespace POS.WinUI.Views.SmartStore;

public partial class PickingRouteControl : UserControl
{
    public PickingRouteControl()
    {
        InitializeComponent();
    }

    private void OnSearchInputKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && DataContext is PickingRouteViewModel vm)
        {
            if (vm.SearchAndAddItemCommand.CanExecute(null))
            {
                vm.SearchAndAddItemCommand.Execute(null);
            }
        }
    }
}
