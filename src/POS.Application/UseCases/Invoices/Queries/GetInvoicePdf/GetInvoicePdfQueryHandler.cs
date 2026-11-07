using MediatR;
using POS.Application.Abstractions.Messaging;
using POS.Application.Abstractions.Reports;
using POS.Application.UseCases.Invoices.DTOs;
using POS.Application.UseCases.Invoices.Errors;
using POS.Application.UseCases.Invoices.Queries.GetReceiptData;
using POS.Domain.Common;

namespace POS.Application.UseCases.Invoices.Queries.GetInvoicePdf;

public sealed class GetInvoicePdfQueryHandler(ISender sender, IInvoicePdfRenderer renderer)
    : IQueryHandler<GetInvoicePdfQuery, InvoicePdfDto>
{
    public async Task<Result<InvoicePdfDto>> Handle(GetInvoicePdfQuery query, CancellationToken cancellationToken)
    {
        var receipt = await sender.Send(new GetReceiptDataQuery(query.Id, Reprint: query.Reprint), cancellationToken);
        if (receipt.IsFailure) return receipt.Error;
        try
        {
            var data = receipt.Value!;
            return new InvoicePdfDto(data.InvoiceId, data.InvoiceNo,
                query.Download ? renderer.Render(data, cancellationToken) : null);
        }
        catch (ReceiptRenderingException) { return InvoiceReadErrors.RenderingFailed; }
    }
}
