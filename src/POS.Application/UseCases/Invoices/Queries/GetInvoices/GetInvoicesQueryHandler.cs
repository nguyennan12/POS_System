using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;
using POS.Application.Abstractions.Persistence;
using POS.Application.UseCases.Invoices.Errors;
using POS.Contracts.V1.Common;
using POS.Contracts.V1.Invoices;
using POS.Domain.Common;

namespace POS.Application.UseCases.Invoices.Queries.GetInvoices;

public class GetInvoicesQueryHandler(
    IInvoiceRepository invoiceRepository,
    IEmployeeRepository employeeRepository,
    ICurrentUser currentUser) : IQueryHandler<GetInvoicesQuery, PagedResponse<InvoiceSummaryResponse>>
{
    public async Task<Result<PagedResponse<InvoiceSummaryResponse>>> Handle(
        GetInvoicesQuery query, CancellationToken cancellationToken)
    {
        if (currentUser.EmployeeId is null) return InvoiceReadErrors.Unauthorized;
        var employee = await employeeRepository.GetByIdAsync(currentUser.EmployeeId.Value, cancellationToken);
        if (employee is null || !employee.IsActive) return InvoiceReadErrors.Unauthorized;

        var filter = query.Filter;
        var (items, total) = await invoiceRepository.GetPagedAsync(
            employee.Id, employee.StoreId, employee.IsChainOwner,
            filter.OrderId, filter.From, filter.To, filter.PageNumber, filter.PageSize, cancellationToken);
        return new PagedResponse<InvoiceSummaryResponse>(items, filter.PageNumber, filter.PageSize, total);
    }
}
