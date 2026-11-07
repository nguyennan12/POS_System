namespace POS.Infrastructure.Payments;

// TODO T58: obtain payment configuration from SystemConfigs through a dedicated abstraction.
public sealed class MomoOptions
{
    public string ApiBaseUrl { get; set; } = "https://test-payment.momo.vn/";
    public string PartnerCode { get; set; } = "";
    public string AccessKey { get; set; } = "";
    public string SecretKey { get; set; } = "";
    public string IpnUrl { get; set; } = "";
    public string RedirectUrl { get; set; } = "";
    public int ReplayToleranceSeconds { get; set; } = 300;
}

public sealed class VietQrOptions
{
    public string BankBin { get; set; } = "";
    public string AccountNo { get; set; } = "";
    public string AccountName { get; set; } = "";
    public string WebhookSecret { get; set; } = "";
    public string? ExpectedGateway { get; set; }
}
