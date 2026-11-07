using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;
using POS.Application.Abstractions.Persistence;
using POS.Application.Abstractions.Reports;
using POS.Application.UseCases.Invoices.DTOs;
using POS.Application.UseCases.Invoices.Errors;
using POS.Application.UseCases.Orders.DTOs;
using POS.Domain.Common;
using POS.Domain.Orders.Enums;

namespace POS.Application.UseCases.Invoices.Queries.GetReceiptData;

public sealed class GetReceiptDataQueryHandler(
    IInvoiceRepository invoices, IEmployeeRepository employees, ICurrentUser currentUser,
    ReceiptSettings settings) : IQueryHandler<GetReceiptDataQuery, InvoiceReceiptDto>
{
    public async Task<Result<InvoiceReceiptDto>> Handle(GetReceiptDataQuery query, CancellationToken cancellationToken)
    {
        if (currentUser.EmployeeId is not Guid employeeId) return InvoiceReadErrors.Unauthorized;
        var employee = await employees.GetByIdAsync(employeeId, cancellationToken);
        if (employee is null || !employee.IsActive || employee.IsLocked(DateTime.UtcNow))
            return InvoiceReadErrors.Unauthorized;

        var invoice = await invoices.GetForReceiptAsync(query.Id, cancellationToken);
        if (invoice is null) return InvoiceReadErrors.InvoiceNotFound;
        var order = invoice.Order;
        // T48 scope is based on the persisted employee, not potentially stale token claims.
        if (!employee.IsChainOwner && employee.StoreId != order.StoreId)
            return InvoiceReadErrors.InvalidStore;
        if (order.Store is null || order.CreatedByEmployee is null)
            return InvoiceReadErrors.ReceiptDataUnavailable;

        TimeZoneInfo timezone;
        try { timezone = TimeZoneInfo.FindSystemTimeZoneById(order.Store.Timezone); }
        catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException)
        { return InvoiceReadErrors.InvalidTimezone; }

        DateTimeOffset Local(DateTime utc) => TimeZoneInfo.ConvertTime(
            new DateTimeOffset(DateTime.SpecifyKind(utc, DateTimeKind.Utc)), timezone);
        var items = order.Items.OrderBy(x => x.Id).Select(x => new OrderItemDto(
            x.Id, x.SkuId, x.Sku?.SkuCode ?? string.Empty, x.Sku?.Product?.Name ?? string.Empty,
            x.Qty, x.UnitPrice, x.DiscountAmount, x.TaxAmount, x.LineTotal)).ToArray();
        var payments = order.Payments.Where(x => x.Status == PaymentStatus.Success)
            .OrderBy(x => x.PaidAt).ThenBy(x => x.Id)
            .Select(x => new OrderPaymentDto(x.Id, x.Method.ToString(), x.Amount, x.ChangeAmount,
                x.TransactionRef, x.Status.ToString(), x.PaidAt.HasValue ? Local(x.PaidAt.Value) : null)).ToArray();
        var (applied, change) = order.GetPaymentTotals();
        var receipt = new ReceiptDataDto(order.Store.Name, order.Store.Address, order.Store.Phone,
            order.Id.ToString("N")[..8].ToUpperInvariant(), order.CreatedByEmployee.Name,
            Local(order.CreatedAt), items, invoice.TotalBeforeTax, order.DiscountTotal,
            invoice.TaxAmount, invoice.GrandTotal, applied, change,
            order.Store.ReceiptHeader, order.Store.ReceiptFooter);
        return new InvoiceReceiptDto(invoice.Id, invoice.InvoiceNo, Local(invoice.IssuedAt),
            invoice.BuyerName, invoice.BuyerTaxCode, invoice.BuyerAddress, receipt, payments,
            query.Reprint, query.PaperWidth ?? settings.DefaultPaperWidth);
    }
}
