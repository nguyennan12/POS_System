using POS.Domain.Common;

namespace POS.Domain.Orders;

public class Invoice : BaseEntity
{
    public Invoice() : base()
    {
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
