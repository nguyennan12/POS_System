using System.Linq.Expressions;
using POS.Contracts.V1.Invoices;
using POS.Domain.Orders;

namespace POS.Application.UseCases.Invoices.Mappings;

public static class InvoiceMappings
{
    public static readonly Expression<Func<Invoice, InvoiceSummaryResponse>> SummaryProjection = i => new(
        i.Id, i.OrderId, i.InvoiceNo, i.BuyerName, i.TotalBeforeTax, i.TaxAmount, i.GrandTotal,
        new DateTimeOffset(i.IssuedAt, TimeSpan.Zero));

    public static InvoiceDetailResponse ToDetailResponse(this Invoice invoice) => new(
        invoice.Id, invoice.OrderId, invoice.InvoiceNo, invoice.BuyerName, invoice.BuyerTaxCode,
        invoice.BuyerAddress, invoice.TotalBeforeTax, invoice.TaxAmount, invoice.GrandTotal,
        new DateTimeOffset(invoice.IssuedAt, TimeSpan.Zero),
        invoice.Order.Items.OrderBy(i => i.Id).Select(i => new InvoiceItemResponse(
            i.SkuId, i.Sku.Product.Name, i.Sku.SkuCode, i.Qty, i.UnitPrice, i.LineTotal)).ToList());
}
