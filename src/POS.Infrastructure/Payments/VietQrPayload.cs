using System.Globalization;
using System.Text;
using POS.Application.UseCases.Payments.Errors;
using POS.Domain.Common;

namespace POS.Infrastructure.Payments;

public static class VietQrPayload
{
    // NAPAS QR Switching v1.5.2: 38/00 GUID, 38/01 beneficiary, 38/02 service;
    // dynamic IBFT to account, VND. Every length is encoded as two decimal digits.
    public static Result<string> Build(string bankBin, string account, decimal amount, string memo)
    {
        if (bankBin.Length != 6 || !bankBin.All(char.IsAsciiDigit)
            || account.Length is < 1 or > 19 || !account.All(char.IsAsciiLetterOrDigit)
            || memo.Length is < 1 or > 25 || !memo.All(char.IsAsciiLetterOrDigit)
            || amount <= 0 || amount != decimal.Truncate(amount))
            return PaymentErrors.GatewayUnavailable;
        var formatted = amount.ToString("0", CultureInfo.InvariantCulture);
        if (formatted.Length > 13) return PaymentErrors.InvalidAmount;
        var beneficiary = Tlv("00", bankBin) + Tlv("01", account);
        var merchant = Tlv("00", "A000000727") + Tlv("01", beneficiary) + Tlv("02", "QRIBFTTA");
        var body = Tlv("00", "01") + Tlv("01", "12") + Tlv("38", merchant) + Tlv("53", "704")
            + Tlv("54", formatted) + Tlv("58", "VN") + Tlv("62", Tlv("08", memo)) + "6304";
        return body + Crc16(body).ToString("X4", CultureInfo.InvariantCulture);
    }

    public static ushort Crc16(string payload)
    {
        ushort crc = 0xFFFF;
        foreach (var b in Encoding.UTF8.GetBytes(payload))
        {
            crc ^= (ushort)(b << 8);
            for (var bit = 0; bit < 8; bit++)
                crc = (ushort)((crc & 0x8000) != 0 ? (crc << 1) ^ 0x1021 : crc << 1);
        }
        return crc;
    }

    private static string Tlv(string tag, string value) => tag + value.Length.ToString("D2", CultureInfo.InvariantCulture) + value;
}
