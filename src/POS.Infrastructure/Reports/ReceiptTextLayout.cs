using System.Globalization;
using System.Text;
using POS.Application.UseCases.Invoices.DTOs;

namespace POS.Infrastructure.Reports;

public sealed record ReceiptLine(string Text, bool Bold = false, bool Centered = false);

public static class ReceiptTextLayout
{
    public static int DisplayWidth(string text) => Elements(text).Sum(Width);

    public static IReadOnlyList<string> Wrap(string text, int columns)
    {
        if (columns < 2) throw new ArgumentOutOfRangeException(nameof(columns));
        var lines = new List<string>();
        foreach (var paragraph in ReceiptFormatting.Text(text).Split('\n'))
        {
            var line = new StringBuilder();
            var width = 0;
            foreach (var element in Elements(paragraph))
            {
                var nextWidth = Width(element);
                if (width + nextWidth > columns)
                {
                    lines.Add(line.ToString());
                    line.Clear();
                    width = 0;
                }
                line.Append(element);
                width += nextWidth;
            }
            lines.Add(line.ToString());
        }
        return lines;
    }

    public static IReadOnlyList<ReceiptLine> Build(InvoiceReceiptDto invoice, int paperWidth)
    {
        var columns = paperWidth switch { 58 => 32, 80 => 48, _ => throw new ArgumentOutOfRangeException(nameof(paperWidth)) };
        var result = new List<ReceiptLine>();
        void Add(string? text, bool bold = false, bool centered = false)
        {
            if (string.IsNullOrWhiteSpace(text)) return;
            result.AddRange(Wrap(text, columns).Select(x => new ReceiptLine(x, bold, centered)));
        }
        void Amount(string label, decimal value, bool bold = false)
        {
            var money = ReceiptFormatting.Money(value);
            var gap = columns - DisplayWidth(label) - DisplayWidth(money);
            if (gap > 0) Add(label + new string(' ', gap) + money, bold);
            else { Add(label, bold); Add(money, bold); }
        }
        var receipt = invoice.Receipt;
        Add(receipt.StoreName, true, true);
        Add(receipt.StoreAddress, centered: true);
        Add(receipt.StorePhone, centered: true);
        Add(receipt.ReceiptHeader, centered: true);
        Add("HÓA ĐƠN BÁN HÀNG", true, true);
        if (invoice.IsReprint) Add("BẢN SAO / REPRINT", true, true);
        Add(invoice.InvoiceNo, true, true);
        Add(ReceiptFormatting.Timestamp(invoice.IssuedAt));
        Add("Thu ngân: " + receipt.CashierName);
        if (invoice.BuyerName is not null) Add("Khách: " + invoice.BuyerName);
        if (invoice.BuyerTaxCode is not null) Add("MST: " + invoice.BuyerTaxCode);
        if (invoice.BuyerAddress is not null) Add("Địa chỉ: " + invoice.BuyerAddress);
        Add(new string('-', columns));
        foreach (var item in receipt.Items)
        {
            Add(item.ProductName, true);
            Add("SL " + ReceiptFormatting.Quantity(item.Qty) + " x " + ReceiptFormatting.Money(item.UnitPrice));
            if (item.DiscountAmount != 0) Amount("Giảm giá", item.DiscountAmount);
            Amount("Thuế", item.TaxAmount);
            Amount("Thành tiền", item.LineTotal);
        }
        Add(new string('-', columns));
        Amount("Tạm tính", receipt.Subtotal);
        Amount("Giảm giá", receipt.DiscountTotal);
        Amount("Thuế", receipt.TaxTotal);
        Amount("TỔNG CỘNG", receipt.GrandTotal, true);
        foreach (var payment in invoice.Payments)
            Amount(ReceiptFormatting.PaymentMethod(payment.Method), payment.Amount);
        Amount("Đã thanh toán", receipt.AmountPaid);
        Amount("Tiền thừa", receipt.ChangeAmount);
        Add(receipt.ReceiptFooter, centered: true);
        return result;
    }

    private static IEnumerable<string> Elements(string text)
    {
        var iterator = StringInfo.GetTextElementEnumerator(ReceiptUnicode.Normalize(text));
        while (iterator.MoveNext()) yield return iterator.GetTextElement();
    }

    private static int Width(string element)
    {
        var value = Rune.GetRuneAt(element, 0).Value;
        // Wide CJK/full-width glyphs and pictographs occupy two receipt cells.
        return value is >= 0x1100 and <= 0x115f or >= 0x2e80 and <= 0xa4cf
            or >= 0xac00 and <= 0xd7a3 or >= 0xf900 and <= 0xfaff
            or >= 0xff01 and <= 0xff60 or >= 0x1f300 and <= 0x1faff ? 2 : 1;
    }
}
