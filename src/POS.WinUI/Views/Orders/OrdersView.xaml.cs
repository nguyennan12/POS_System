using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using POS.WinUI.ViewModels.Orders;

namespace POS.WinUI.Views.Orders;

public partial class OrdersView : UserControl
{
    public OrdersView()
    {
        InitializeComponent();
        Unloaded += (_, _) => { if (DataContext is OrdersViewModel vm) vm.Deactivate(); };
        ReprintDialog.IsVisibleChanged += (_, _) =>
        {
            if (ReprintDialog.IsVisible) ReprintDialog.MoveFocus(new TraversalRequest(FocusNavigationDirection.First));
            else Focus();
        };
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        UpdateLayoutForSize();
        if (DataContext is OrdersViewModel vm) await vm.InitializeAsync();
    }
    private void OnSizeChanged(object sender, SizeChangedEventArgs e) => UpdateLayoutForSize();
    private void UpdateLayoutForSize()
    {
        if (Body == null) return;
        var stacked = ActualWidth < 1080;
        ListColumn.Width = new GridLength(stacked ? 1 : 1.35, GridUnitType.Star);
        DetailColumn.Width = stacked ? new GridLength(0) : new GridLength(1, GridUnitType.Star);
        Body.Height = stacked ? 1240 : Math.Max(300, ActualHeight - 470);
        ListCard.Height = stacked ? 600 : double.NaN;
        DetailCard.Height = stacked ? 620 : double.NaN;
        Grid.SetRow(DetailCard, stacked ? 1 : 0);
        Grid.SetColumn(DetailCard, stacked ? 0 : 1);
        ListCard.Margin = stacked ? new Thickness(0, 0, 0, 16) : new Thickness(0, 0, 16, 0);
    }
    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (DataContext is not OrdersViewModel vm) return;
        if (e.Key == Key.F5 && !vm.IsReprintOpen)
        { if (vm.RefreshCommand.CanExecute(null)) vm.RefreshCommand.Execute(null); e.Handled = true; return; }
        if (e.Key == Key.Escape && vm.IsReprintOpen)
        { if (vm.CloseReprintCommand.CanExecute(null)) vm.CloseReprintCommand.Execute(null); e.Handled = true; }
        else if (e.Key == Key.P && Keyboard.Modifiers == ModifierKeys.Control && !vm.IsReprintOpen && vm.OpenReprintCommand.CanExecute(null))
        { vm.OpenReprintCommand.Execute(null); e.Handled = true; }
    }
}
