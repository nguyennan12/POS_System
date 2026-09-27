using POS.Domain.Common;
using POS.Domain.Products;

namespace POS.Domain.Orders;

public class OrderItem : BaseEntity
{
    public OrderItem() : base()
    {
    }

    public OrderItem(
        Guid orderId,
        Guid skuId,
        decimal qty,
        decimal unitPrice,
        Guid? id = null) : base(id)
    {
        OrderId = orderId;
        SkuId = skuId;
        Qty = qty;
        UnitPrice = unitPrice;
        DiscountAmount = 0;
        TaxAmount = 0;
        LineTotal = Math.Round(unitPrice * qty, 2);
    }

    public Guid OrderId { get; private set; }
    public Order Order { get; private set; } = default!;

    public Guid SkuId { get; private set; }
    public Sku Sku { get; set; } = default!;

    public decimal Qty { get; private set; }
    public decimal UnitPrice { get; private set; }
    public decimal DiscountAmount { get; private set; }
    public decimal TaxAmount { get; private set; }
    public decimal LineTotal { get; private set; }

    public void UpdateQty(decimal qty)
    {
        if (qty <= 0)
            throw new ArgumentOutOfRangeException(nameof(qty), "Số lượng sản phẩm phải lớn hơn 0.");

        Qty = qty;
        LineTotal = Math.Max(0, (UnitPrice * Qty) - DiscountAmount) + TaxAmount;
    }

    public void AddQty(decimal additionalQty)
    {
        if (additionalQty <= 0)
            throw new ArgumentOutOfRangeException(nameof(additionalQty), "Số lượng thêm phải lớn hơn 0.");

        Qty += additionalQty;
        LineTotal = Math.Max(0, (UnitPrice * Qty) - DiscountAmount) + TaxAmount;
    }

    public void ApplyDiscountAndTax(decimal discountAmount, decimal taxRate)
    {
        decimal lineGross = Math.Round(UnitPrice * Qty, 2);
        DiscountAmount = Math.Min(lineGross, Math.Max(0, discountAmount));
        decimal lineNet = Math.Max(0, lineGross - DiscountAmount);
        
        TaxAmount = Math.Round(lineNet * (Math.Max(0, taxRate) / 100m), 2);
        LineTotal = Math.Round(lineNet + TaxAmount, 2);
    }
}
