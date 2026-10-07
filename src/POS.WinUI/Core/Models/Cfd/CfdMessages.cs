using POS.WinUI.Core.Models.Cfd;

namespace POS.WinUI.Core.Models.Cfd;

public record CfdCartStateMessage(CfdCartStateDto State);
public record CfdPaymentStateMessage(CfdPaymentStateDto State);
public record CfdClosePaymentMessage();
public record CfdPaymentQrMessage(CfdPaymentQrDto PaymentInfo);
public record CfdShowStandbyMessage();
public record CfdThankYouMessage(string CustomerName, decimal AmountPaid);
