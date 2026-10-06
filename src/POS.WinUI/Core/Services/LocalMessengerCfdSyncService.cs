using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Messaging;
using POS.WinUI.Core.Models.Cfd;

namespace POS.WinUI.Core.Services;

/// <summary>
/// Triển khai đồng bộ CFD bằng In-Memory Messenger (Local trên cùng 1 máy mở 2 màn hình).
/// Có thể chuyển đổi sang SignalR mà không cần thay đổi ViewModel thu ngân.
/// </summary>
public sealed class LocalMessengerCfdSyncService : ICfdSyncService
{
    public Task SyncCartAsync(CfdCartStateDto cartState)
    {
        if (cartState.TotalItems > 0)
        {
            WeakReferenceMessenger.Default.Send(new CfdCartStateMessage(cartState));
        }
        else
        {
            WeakReferenceMessenger.Default.Send(new CfdShowStandbyMessage());
        }

        return Task.CompletedTask;
    }

    public Task ShowStandbyAsync()
    {
        WeakReferenceMessenger.Default.Send(new CfdShowStandbyMessage());
        return Task.CompletedTask;
    }

    public Task SyncPaymentStateAsync(CfdPaymentStateDto paymentState)
    {
        WeakReferenceMessenger.Default.Send(new CfdPaymentStateMessage(paymentState));
        return Task.CompletedTask;
    }

    public Task ClosePaymentAsync()
    {
        WeakReferenceMessenger.Default.Send(new CfdClosePaymentMessage());
        return Task.CompletedTask;
    }

    public Task ShowPaymentQrAsync(CfdPaymentQrDto paymentInfo)
    {
        WeakReferenceMessenger.Default.Send(new CfdPaymentQrMessage(paymentInfo));
        return Task.CompletedTask;
    }

    public Task ShowThankYouAsync(string customerName, decimal amountPaid)
    {
        WeakReferenceMessenger.Default.Send(new CfdThankYouMessage(customerName, amountPaid));
        return Task.CompletedTask;
    }
}
