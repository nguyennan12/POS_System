namespace POS.Api.Options;

public sealed class PaymentHubOptions
{
    public const string SectionName = "PaymentHub";
    public string[] AllowedOrigins { get; set; } = [];
}
