using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using POS.WinUI.Core.Constants;
using POS.WinUI.ViewModels.Orders;

namespace POS.WinUI.ViewModels.Cashier;

public partial class PosCashierMainViewModel
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsInvoiceViewerOpen))]
    private OrdersViewModel? _invoiceViewer;
    public bool IsInvoiceViewerOpen => InvoiceViewer != null;
    private bool CanViewInvoiceHistory() => _sessionService.HasPermission(AppPermissions.Invoices.Read);

    [RelayCommand(CanExecute = nameof(CanViewInvoiceHistory))]
    private void OpenInvoices()
    {
        // Overlay preserves the current cart and shift instead of navigating to a new cashier instance.
        if (InvoiceViewer != null || IsPaymentModalOpen || IsShiftModalOpen) return;
        InvoiceViewer = App.Services.GetRequiredService<OrdersViewModel>();
    }

    [RelayCommand]
    private void CloseInvoices()
    {
        if (InvoiceViewer?.IsOutputBusy == true) return;
        InvoiceViewer?.Dispose(); InvoiceViewer = null;
    }
}
