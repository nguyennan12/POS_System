using MediatR;
using POS.Application.Abstractions.Messaging;
using POS.Application.Abstractions.Reports;
using POS.Application.UseCases.Invoices.Errors;
using POS.Application.UseCases.Invoices.Queries.GetReceiptData;
using POS.Domain.Common;

namespace POS.Application.UseCases.Invoices.Queries.GetEscPosReceipt;

public sealed class GetEscPosReceiptQueryHandler(ISender sender, IThermalReceiptRenderer renderer)
    : IQueryHandler<GetEscPosReceiptQuery, byte[]>
{
    public async Task<Result<byte[]>> Handle(GetEscPosReceiptQuery query, CancellationToken cancellationToken)
    {
        var receipt = await sender.Send(new GetReceiptDataQuery(query.Id, query.PaperWidth, query.Reprint), cancellationToken);
        if (receipt.IsFailure) return receipt.Error;
        try { return renderer.Render(receipt.Value!, receipt.Value!.PaperWidth, cancellationToken); }
        catch (ReceiptRenderingException) { return InvoiceReadErrors.RenderingFailed; }
    }
}
