using POS.WinUI.ViewModels.Orders;

namespace POS.WinUI.Views.Orders;

// T52 entry point; the management tab and standalone preview share the same view.
public sealed class frmInvoiceView : OrdersView
{
    public frmInvoiceView(OrdersViewModel viewModel) => DataContext = viewModel;
}
