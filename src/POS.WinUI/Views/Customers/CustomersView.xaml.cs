using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using POS.WinUI.ViewModels.Customers;

namespace POS.WinUI.Views.Customers;

public partial class CustomersView : UserControl
{
    public CustomersView()
    {
        InitializeComponent();
        DataContextChanged += async (_, e) =>
        {
            if (e.NewValue is CustomersViewModel vm)
            {
                await vm.InitializeAsync();
            }
        };
        Loaded += async (_, _) =>
        {
            if (DataContext is CustomersViewModel vm)
            {
                await vm.InitializeAsync();
            }
        };
    }

    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        base.OnPreviewKeyDown(e);
        if (e.Key == Key.Escape && DataContext is CustomersViewModel vm)
        {
            if (vm.IsCustomerModalOpen || vm.IsPointModalOpen || vm.IsTierModalOpen)
            {
                vm.CloseModalCommand.Execute(null);
                vm.CloseTierModalCommand.Execute(null);
                e.Handled = true;
            }
        }
    }
}
