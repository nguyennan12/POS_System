using System.Threading.Tasks;
using POS.WinUI.Core.Models.Cfd;

namespace POS.WinUI.Core.Services;

public interface ICfdSyncService
{
    Task SyncCartAsync(CfdCartStateDto cartState);
    Task ShowStandbyAsync();
    Task ShowPaymentQrAsync(CfdPaymentQrDto paymentInfo);
    Task ShowThankYouAsync(string customerName, decimal amountPaid);
}
