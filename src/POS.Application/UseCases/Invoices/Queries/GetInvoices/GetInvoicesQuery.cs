using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;
using POS.Contracts.V1.Common;
using POS.Contracts.V1.Invoices;

namespace POS.Application.UseCases.Invoices.Queries.GetInvoices;

public record GetInvoicesQuery(InvoiceFilterRequest Filter)
    : IQuery<PagedResponse<InvoiceSummaryResponse>>, IRequirePermission
{
    public string RequiredPermission => "invoices:read";
}
