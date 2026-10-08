using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;
using POS.Contracts.V1.Invoices;

namespace POS.Application.UseCases.Invoices.Queries.GetInvoiceById;

public record GetInvoiceByIdQuery(Guid Id) : IQuery<InvoiceDetailResponse>, IRequirePermission
{
    public string RequiredPermission => "invoices:read";
}
