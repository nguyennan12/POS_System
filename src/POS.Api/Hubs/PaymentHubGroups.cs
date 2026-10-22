namespace POS.Api.Hubs;

internal static class PaymentHubGroups
{
    public static string ForPayment(Guid paymentId) => $"payment:{paymentId:D}";
}
