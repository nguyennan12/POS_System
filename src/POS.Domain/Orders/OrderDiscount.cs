using POS.Domain.Common;
using POS.Domain.Promotions;

namespace POS.Domain.Orders;

public class OrderDiscount : BaseEntity
{
    public OrderDiscount() : base()
    {
    }

    public OrderDiscount(
        Guid orderId,
        Guid? promotionId,
        Guid? voucherId,
        decimal discountAmount,
        string? description,
        DateTime appliedAt,
        Guid? id = null) : base(id)
    {
        OrderId = orderId;
        PromotionId = promotionId;
        VoucherId = voucherId;
        DiscountAmount = Math.Max(0, discountAmount);
        Description = description;
        AppliedAt = appliedAt;
    }

    public Guid OrderId { get; private set; }
    public Order Order { get; private set; } = default!;

    public Guid? PromotionId { get; private set; }
    public Promotion? Promotion { get; private set; }

    public Guid? VoucherId { get; private set; }
    public Voucher? Voucher { get; private set; }

    public decimal DiscountAmount { get; private set; }
    public string? Description { get; private set; }
    public DateTime AppliedAt { get; private set; } = DateTime.UtcNow;
}
