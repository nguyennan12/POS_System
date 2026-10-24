using POS.Application.UseCases.Invoices.DTOs;
using POS.Contracts.V1.Invoices;
using POS.Contracts.V1.Orders;

namespace POS.Api.Mappings;

public static class InvoiceMapping
{
    public static InvoicePdfResponse ToResponse(this InvoicePdfDto dto, string downloadUrl) =>
        new(dto.InvoiceId, dto.InvoiceNo, downloadUrl);

    public static ReceiptDataResponse ToResponse(this InvoiceReceiptDto dto)
    {
        var receipt = dto.Receipt;
        return new ReceiptDataResponse(receipt.StoreName, receipt.StoreAddress, receipt.StorePhone,
            receipt.OrderNo, receipt.CashierName, receipt.CreatedAt,
            receipt.Items.Select(x => new OrderItemResponse(x.Id, x.SkuId, x.SkuCode,
                x.ProductName, x.Qty, x.UnitPrice, x.DiscountAmount, x.TaxAmount, x.LineTotal)).ToArray(),
            receipt.Subtotal, receipt.DiscountTotal, receipt.TaxTotal, receipt.GrandTotal,
            receipt.AmountPaid, receipt.ChangeAmount, receipt.ReceiptHeader, receipt.ReceiptFooter)
        {
            InvoiceId = dto.InvoiceId, InvoiceNo = dto.InvoiceNo, IssuedAt = dto.IssuedAt,
            BuyerName = dto.BuyerName, BuyerTaxCode = dto.BuyerTaxCode, BuyerAddress = dto.BuyerAddress,
            Payments = dto.Payments.Select(x => new OrderPaymentResponse(x.Id, x.Method,
                x.Amount, x.ChangeAmount, x.TransactionRef, x.Status, x.PaidAt)).ToArray(),
            IsReprint = dto.IsReprint, PaperWidth = dto.PaperWidth
        };
    }
}
