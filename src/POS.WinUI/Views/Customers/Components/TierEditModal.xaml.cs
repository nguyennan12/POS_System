using System.Windows.Controls;
using System.Windows.Input;
using POS.WinUI.ViewModels.Customers;

namespace POS.WinUI.Views.Customers.Components;

public partial class TierEditModal : UserControl
{
    public TierEditModal()
    {
        InitializeComponent();
    }

    private void OnBackdropMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is CustomersViewModel vm)
        {
            vm.CloseTierModalCommand.Execute(null);
        }
    }

    private void OnCardMouseDown(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
    }
}
