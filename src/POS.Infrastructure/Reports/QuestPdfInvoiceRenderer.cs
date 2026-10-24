using Microsoft.Extensions.Options;
using POS.Application.Abstractions.Reports;
using POS.Application.UseCases.Invoices.DTOs;
using QuestPDF.Fluent;

namespace POS.Infrastructure.Reports;

public sealed class QuestPdfInvoiceRenderer(IOptions<ReceiptOptions> options) : IInvoicePdfRenderer
{
    public byte[] Render(InvoiceReceiptDto invoice, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ReceiptFontRegistration.EnsureRegistered();
        try
        {
            var bytes = InvoiceDocument.Create(invoice, options.Value.FontFamily).GeneratePdf();
            cancellationToken.ThrowIfCancellationRequested();
            return bytes;
        }
        catch (Exception ex) when (ex is not OperationCanceledException and not OutOfMemoryException)
        { throw new ReceiptRenderingException(); }
    }
}
