using System.Windows.Controls;
using System.Windows.Input;
using POS.WinUI.ViewModels.Promotions;

namespace POS.WinUI.Views.Promotions.Components;

public partial class PromotionEditModal : UserControl
{
    public PromotionEditModal()
    {
        InitializeComponent();
    }

    private void OnBackdropMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is PromotionsViewModel vm)
        {
            vm.CloseModalCommand.Execute(null);
        }
    }

    private void OnCardMouseDown(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
    }
}
