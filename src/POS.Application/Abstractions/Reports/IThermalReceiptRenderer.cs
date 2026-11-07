using POS.Application.UseCases.Invoices.DTOs;

namespace POS.Application.Abstractions.Reports;

public interface IThermalReceiptRenderer
{
    byte[] Render(InvoiceReceiptDto invoice, int paperWidth, CancellationToken cancellationToken = default);
}

public sealed record ReceiptSettings(int DefaultPaperWidth = 80);

// Infrastructure wraps rendering failures without exposing document content in errors/logs.
public sealed class ReceiptRenderingException : Exception
{
    public ReceiptRenderingException() : base("Could not render invoice.") { }
}
