using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using POS.WinUI.ViewModels.Cashier;

namespace POS.WinUI.Views.Cashier;

public partial class PosCashierMainView : UserControl
{
  public PosCashierMainView(PosCashierMainViewModel viewModel)
  {
    InitializeComponent();
    DataContext = viewModel;
    Loaded += (_, _) => Focus();
    InvoiceOverlay.IsVisibleChanged += (_, _) =>
    {
      if (InvoiceOverlay.IsVisible) InvoiceOverlay.MoveFocus(new TraversalRequest(FocusNavigationDirection.First));
      else Focus();
    };
    PreviewKeyDown += (_, e) =>
    {
      if (!viewModel.IsInvoiceViewerOpen) return;
      if (e.Key == Key.F5 && viewModel.InvoiceViewer?.IsReprintOpen == false && viewModel.InvoiceViewer.RefreshCommand.CanExecute(null))
        viewModel.InvoiceViewer.RefreshCommand.Execute(null);
      if (e.Key is Key.F1 or Key.F5 or Key.F6 or Key.F7) e.Handled = true;
      if (e.Key == Key.Escape && viewModel.InvoiceViewer?.IsReprintOpen != true)
      { viewModel.CloseInvoicesCommand.Execute(null); e.Handled = true; }
    };
  }


  /// Khi bấm thêm sản phẩm, kích hoạt hiệu ứng nảy nhẹ biểu tượng giỏ hàng phản hồi
  /// </summary>
  private void OnProductCardClick(object sender, RoutedEventArgs e)
  {
    CartView?.PlayBounceAnimation();
  }
}
