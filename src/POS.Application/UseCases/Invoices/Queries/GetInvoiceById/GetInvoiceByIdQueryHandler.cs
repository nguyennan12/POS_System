using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;
using POS.Application.Abstractions.Persistence;
using POS.Application.UseCases.Invoices.Errors;
using POS.Application.UseCases.Invoices.Mappings;
using POS.Contracts.V1.Invoices;
using POS.Domain.Common;

namespace POS.Application.UseCases.Invoices.Queries.GetInvoiceById;

public class GetInvoiceByIdQueryHandler(
    IInvoiceRepository invoiceRepository,
    IEmployeeRepository employeeRepository,
    ICurrentUser currentUser) : IQueryHandler<GetInvoiceByIdQuery, InvoiceDetailResponse>
{
    public async Task<Result<InvoiceDetailResponse>> Handle(GetInvoiceByIdQuery query, CancellationToken cancellationToken)
    {
        if (currentUser.EmployeeId is null) return InvoiceReadErrors.Unauthorized;
        var employee = await employeeRepository.GetByIdAsync(currentUser.EmployeeId.Value, cancellationToken);
        if (employee is null || !employee.IsActive) return InvoiceReadErrors.Unauthorized;

        var invoice = await invoiceRepository.GetByIdWithDetailsAsync(query.Id, cancellationToken);
        if (invoice is null) return InvoiceReadErrors.InvoiceNotFound;
        if (!employee.IsChainOwner && employee.StoreId != invoice.Order.StoreId)
            return InvoiceReadErrors.InvalidStore;

        return invoice.ToDetailResponse();
    }
}
