using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;
using POS.Application.UseCases.Invoices.DTOs;

namespace POS.Application.UseCases.Invoices.Queries.GetReceiptData;

public sealed record GetReceiptDataQuery(Guid Id, int? PaperWidth = null, bool Reprint = false)
    : IQuery<InvoiceReceiptDto>, IRequirePermission
{
    public string RequiredPermission => "invoices:read";
}
