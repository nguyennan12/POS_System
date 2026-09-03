using POS.Domain.Common;
using POS.Domain.Products;

namespace POS.Domain.Orders;

public class OrderItem : BaseEntity
{
    public OrderItem() : base()
    {
    }

    public Guid OrderId { get; private set; }
    public Order Order { get; private set; } = default!;

    public Guid SkuId { get; private set; }
    public Sku Sku { get; private set; } = default!;

    public decimal Qty { get; private set; }
    public decimal UnitPrice { get; private set; }
    public decimal DiscountAmount { get; private set; }
    public decimal TaxAmount { get; private set; }
    public decimal LineTotal { get; private set; }
}
