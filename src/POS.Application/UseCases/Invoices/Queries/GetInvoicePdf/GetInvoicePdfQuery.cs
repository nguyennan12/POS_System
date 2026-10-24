using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;
using POS.Application.UseCases.Invoices.DTOs;

namespace POS.Application.UseCases.Invoices.Queries.GetInvoicePdf;

public sealed record GetInvoicePdfQuery(Guid Id, bool Reprint = false, bool Download = false)
    : IQuery<InvoicePdfDto>, IRequirePermission
{
    public string RequiredPermission => "invoices:read";
}
