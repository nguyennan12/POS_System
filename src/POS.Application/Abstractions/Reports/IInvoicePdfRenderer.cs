using POS.Application.UseCases.Invoices.DTOs;

namespace POS.Application.Abstractions.Reports;

public interface IInvoicePdfRenderer
{
    byte[] Render(InvoiceReceiptDto invoice, CancellationToken cancellationToken = default);
}
