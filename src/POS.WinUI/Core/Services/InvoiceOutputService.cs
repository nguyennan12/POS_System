using System.IO;
using System.Printing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using Microsoft.Extensions.Configuration;
using Microsoft.Win32;
using POS.Contracts.V1.Orders;
using POS.WinUI.ViewModels.Orders;

namespace POS.WinUI.Core.Services;

public interface IInvoiceOutputService
{
    IReadOnlyList<string> GetPrinterNames();
    bool PrintReceipt(ReceiptDataResponse receipt, string printerName, int copies);
    bool SavePdf(byte[] bytes, string invoiceNo);
}

public sealed class InvoiceOutputService(IConfiguration configuration) : IInvoiceOutputService
{
    public const string ChoosePrinter = "Chọn máy in trong hộp thoại…";

    public IReadOnlyList<string> GetPrinterNames()
    {
        using var server = new LocalPrintServer();
        using var queues = server.GetPrintQueues();
        var names = queues.Select(q => q.Name).OrderBy(x => x).ToList();
        var configured = configuration["PrinterSettings:PrinterName"];
        if (configured != null && names.Remove(configured)) names.Insert(0, configured);
        names.Add(ChoosePrinter);
        return names;
    }

    public bool PrintReceipt(ReceiptDataResponse receipt, string printerName, int copies)
    {
        if (!receipt.IsReprint) throw new InvalidOperationException("Phiếu in lại phải có dấu bản sao.");
        if (copies is < 1 or > 3) throw new InvalidOperationException("Số bản in phải từ 1 đến 3.");
        var dialog = new PrintDialog();
        using var server = new LocalPrintServer();
        using var configuredQueue = printerName == ChoosePrinter ? null : server.GetPrintQueue(printerName);
        if (printerName == ChoosePrinter)
        {
            if (dialog.ShowDialog() != true) return false;
        }
        else
        {
            dialog.PrintQueue = configuredQueue;
            dialog.PrintTicket = configuredQueue!.DefaultPrintTicket;
        }
        var width = (receipt.PaperWidth == 58 ? 58 : 80) * 96d / 25.4;
        var document = CreateReceiptDocument(receipt, Math.Min(width, dialog.PrintableAreaWidth));
        document.PageHeight = dialog.PrintableAreaHeight;
        // A failure is surfaced without automatic retry, which could print duplicate copies.
        dialog.PrintTicket ??= new PrintTicket();
        dialog.PrintTicket.CopyCount = copies;
        dialog.PrintDocument(((IDocumentPaginatorSource)document).DocumentPaginator, $"BẢN SAO - {receipt.InvoiceNo}");
        return true;
    }

    public static FlowDocument CreateReceiptDocument(ReceiptDataResponse receipt, double width)
    {
        var document = new FlowDocument
        {
            FontFamily = new FontFamily("Segoe UI"), FontSize = 11,
            PageWidth = width, ColumnWidth = width, PagePadding = new Thickness(10),
            Foreground = Brushes.Black, Background = Brushes.White
        };
        void Line(string text, bool bold = false, bool center = false) => document.Blocks.Add(
            new Paragraph(new Run(text))
            {
                Margin = new Thickness(0, 0, 0, 5), FontWeight = bold ? FontWeights.Bold : FontWeights.Normal,
                TextAlignment = center ? TextAlignment.Center : TextAlignment.Left
            });
        static string Money(decimal amount) => InvoiceViewItem.Money(amount);
        Line(receipt.StoreName, true, true);
        foreach (var value in new[] { receipt.StoreAddress, receipt.StorePhone, receipt.ReceiptHeader })
            if (!string.IsNullOrWhiteSpace(value)) Line(value, center: true);
        Line("HÓA ĐƠN BÁN LẺ", true, true);
        Line("BẢN SAO / REPRINT", true, true);
        Line(receipt.InvoiceNo ?? receipt.OrderNo, true);
        Line($"Ngày: {receipt.IssuedAt ?? receipt.CreatedAt:dd/MM/yyyy HH:mm}");
        Line($"Thu ngân: {receipt.CashierName}");
        Line($"Khách: {receipt.BuyerName ?? "Khách lẻ"}");
        if (!string.IsNullOrWhiteSpace(receipt.BuyerTaxCode)) Line($"MST: {receipt.BuyerTaxCode}");
        if (!string.IsNullOrWhiteSpace(receipt.BuyerAddress)) Line(receipt.BuyerAddress);
        Line("────────────────────────");
        foreach (var item in receipt.Items)
        {
            Line(item.ProductName, true);
            Line($"{item.Qty:0.###} × {Money(item.UnitPrice)}    {Money(item.LineTotal)}");
        }
        Line("────────────────────────");
        Line($"Tạm tính: {Money(receipt.Subtotal)}");
        Line($"Giảm giá: {Money(receipt.DiscountTotal)}");
        Line($"Thuế: {Money(receipt.TaxTotal)}");
        Line($"TỔNG CỘNG: {Money(receipt.GrandTotal)}", true);
        foreach (var payment in receipt.Payments) Line($"{InvoiceViewItem.PaymentLabel(payment.Method)}: {Money(payment.Amount)}");
        Line($"Đã thanh toán: {Money(receipt.AmountPaid)}");
        Line($"Tiền thừa: {Money(receipt.ChangeAmount)}");
        if (!string.IsNullOrWhiteSpace(receipt.ReceiptFooter)) Line(receipt.ReceiptFooter, center: true);
        return document;
    }

    public bool SavePdf(byte[] bytes, string invoiceNo)
    {
        var safeName = string.Concat(invoiceNo.Select(c => Path.GetInvalidFileNameChars().Contains(c) ? '_' : c));
        var dialog = new SaveFileDialog
        {
            Title = "Lưu hóa đơn bản sao", Filter = "PDF (*.pdf)|*.pdf", DefaultExt = ".pdf",
            FileName = safeName + "-REPRINT.pdf", AddExtension = true
        };
        if (dialog.ShowDialog() != true) return false;
        File.WriteAllBytes(dialog.FileName, bytes);
        return true;
    }
}
