using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using POS.WinUI.ViewModels.Promotions;

namespace POS.WinUI.Views.Promotions;

public partial class PromotionsView : UserControl
{
    public PromotionsView()
    {
        InitializeComponent();
        DataContextChanged += async (_, e) =>
        {
            if (e.NewValue is PromotionsViewModel vm)
            {
                await vm.InitializeAsync();
            }
        };
        Loaded += async (_, _) =>
        {
            if (DataContext is PromotionsViewModel vm)
            {
                await vm.InitializeAsync();
            }
        };
    }

    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        base.OnPreviewKeyDown(e);
        if (e.Key == Key.Escape && DataContext is PromotionsViewModel vm)
        {
            if (vm.IsPromotionModalOpen || vm.IsVoucherModalOpen)
            {
                vm.CloseModalCommand.Execute(null);
                e.Handled = true;
            }
        }
    }
}
