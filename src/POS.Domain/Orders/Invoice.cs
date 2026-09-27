using POS.Domain.Common;

namespace POS.Domain.Orders;

public class Invoice : BaseEntity
{
    public Invoice() : base()
    {
    }

    private Invoice(
        Guid orderId,
        string invoiceNo,
        decimal totalBeforeTax,
        decimal taxAmount,
        decimal grandTotal,
        string? buyerName = null,
        string? buyerTaxCode = null,
        string? buyerAddress = null,
        Guid? id = null) : base(id)
    {
        OrderId = orderId;
        InvoiceNo = invoiceNo;
        TotalBeforeTax = totalBeforeTax;
        TaxAmount = taxAmount;
        GrandTotal = grandTotal;
        BuyerName = buyerName;
        BuyerTaxCode = buyerTaxCode;
        BuyerAddress = buyerAddress;
        IssuedAt = DateTime.UtcNow;
    }

    public static Invoice Create(
        Guid orderId,
        string invoiceNo,
        decimal subtotal,
        decimal taxAmount,
        decimal grandTotal,
        string? buyerName = null,
        string? buyerTaxCode = null,
        string? buyerAddress = null)
    {
        return new Invoice(orderId, invoiceNo, subtotal, taxAmount, grandTotal,
            buyerName, buyerTaxCode, buyerAddress);
    }

    public Guid OrderId { get; private set; }
    public Order Order { get; private set; } = default!;

    public string InvoiceNo { get; private set; } = default!;
    public string? BuyerName { get; private set; }
    public string? BuyerTaxCode { get; private set; }
    public string? BuyerAddress { get; private set; }
    public decimal TotalBeforeTax { get; private set; }
    public decimal TaxAmount { get; private set; }
    public decimal GrandTotal { get; private set; }
    public DateTime IssuedAt { get; private set; } = DateTime.UtcNow;
}
