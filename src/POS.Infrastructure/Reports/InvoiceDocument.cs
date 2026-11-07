using POS.Application.UseCases.Invoices.DTOs;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace POS.Infrastructure.Reports;

internal static class InvoiceDocument
{
    public static IDocument Create(InvoiceReceiptDto invoice, string fontFamily) => Document.Create(document =>
    {
        var receipt = invoice.Receipt;
        document.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(28);
            page.DefaultTextStyle(x => x.FontFamily(fontFamily).FontSize(9).BreakAnywhere());
            page.Header().Column(column =>
            {
                column.Item().AlignCenter().Text(ReceiptFormatting.Text(receipt.StoreName)).Bold().FontSize(15);
                foreach (var text in new[] { receipt.StoreAddress, receipt.StorePhone, receipt.ReceiptHeader })
                    if (!string.IsNullOrWhiteSpace(text)) column.Item().AlignCenter().Text(ReceiptFormatting.Text(text));
                column.Item().PaddingVertical(6).AlignCenter().Text("HÓA ĐƠN BÁN HÀNG").Bold().FontSize(13);
                if (invoice.IsReprint) column.Item().AlignCenter().Text("BẢN SAO / REPRINT").Bold();
                column.Item().Text(ReceiptFormatting.Text(invoice.InvoiceNo)).Bold();
                column.Item().Text(ReceiptFormatting.Timestamp(invoice.IssuedAt));
                column.Item().Text("Thu ngân: " + ReceiptFormatting.Text(receipt.CashierName));
                if (invoice.BuyerName is not null) column.Item().Text("Người mua: " + ReceiptFormatting.Text(invoice.BuyerName));
                if (invoice.BuyerTaxCode is not null) column.Item().Text("MST: " + ReceiptFormatting.Text(invoice.BuyerTaxCode));
                if (invoice.BuyerAddress is not null) column.Item().Text("Địa chỉ: " + ReceiptFormatting.Text(invoice.BuyerAddress));
            });
            page.Content().PaddingVertical(10).Column(column =>
            {
                column.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(3);
                        columns.RelativeColumn(0.7f);
                        for (var i = 0; i < 4; i++) columns.RelativeColumn(1.5f);
                    });
                    table.Header(header =>
                    {
                        foreach (var text in new[] { "Hàng hóa", "SL", "Đơn giá", "Giảm giá", "Thuế", "Thành tiền" })
                            header.Cell().BorderBottom(1).Padding(3).Text(text).Bold();
                    });
                    foreach (var item in receipt.Items)
                    {
                        table.Cell().BorderBottom(0.3f).Padding(3).Text(ReceiptFormatting.Text(item.ProductName));
                        table.Cell().BorderBottom(0.3f).Padding(3).AlignRight().Text(ReceiptFormatting.Quantity(item.Qty));
                        foreach (var amount in new[] { item.UnitPrice, item.DiscountAmount, item.TaxAmount, item.LineTotal })
                            table.Cell().BorderBottom(0.3f).Padding(3).AlignRight().Text(ReceiptFormatting.Money(amount));
                    }
                });
                void Amount(string label, decimal amount, bool bold = false)
                {
                    column.Item().PaddingTop(4).Row(row =>
                    {
                        var left = row.RelativeItem().Text(label);
                        var right = row.RelativeItem().AlignRight().Text(ReceiptFormatting.Money(amount));
                        if (bold) { left.Bold(); right.Bold(); }
                    });
                }
                Amount("Tạm tính", receipt.Subtotal);
                Amount("Giảm giá", receipt.DiscountTotal);
                Amount("Thuế", receipt.TaxTotal);
                Amount("TỔNG CỘNG", receipt.GrandTotal, true);
                foreach (var payment in invoice.Payments) Amount(ReceiptFormatting.PaymentMethod(payment.Method), payment.Amount);
                Amount("Đã thanh toán", receipt.AmountPaid);
                Amount("Tiền thừa", receipt.ChangeAmount);
            });
            page.Footer().Column(column =>
            {
                if (!string.IsNullOrWhiteSpace(receipt.ReceiptFooter))
                    column.Item().AlignCenter().Text(ReceiptFormatting.Text(receipt.ReceiptFooter));
                column.Item().AlignCenter().Text(text => { text.Span("Trang "); text.CurrentPageNumber(); text.Span(" / "); text.TotalPages(); });
            });
        });
    });
}
