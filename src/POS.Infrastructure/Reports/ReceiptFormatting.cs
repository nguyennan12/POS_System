using System.Globalization;
using System.Text;

namespace POS.Infrastructure.Reports;

public static class ReceiptFormatting
{
    private static readonly NumberFormatInfo VietnameseNumbers = new()
    {
        NumberGroupSeparator = ".", NumberDecimalSeparator = ",", NumberGroupSizes = [3]
    };

    // Explicit number data also works when named cultures are unavailable in invariant mode.
    public static string Money(decimal value) => value.ToString(
        value == decimal.Truncate(value) ? "N0" : "N2", VietnameseNumbers) + " ₫";
    public static string Quantity(decimal value) => value.ToString("0.###", VietnameseNumbers);
    public static string Timestamp(DateTimeOffset value) => value.ToString("dd/MM/yyyy HH:mm:ss zzz", CultureInfo.InvariantCulture);
    public static string Text(string? value) => string.Concat(ReceiptUnicode.Normalize(value ?? string.Empty)
        .Where(c => !char.IsControl(c) || c == '\n'));
    public static string PaymentMethod(string method) => method switch
    {
        "Cash" => "Tiền mặt", "Card" => "Thẻ", "Points" => "Điểm", _ => Text(method)
    };
}
