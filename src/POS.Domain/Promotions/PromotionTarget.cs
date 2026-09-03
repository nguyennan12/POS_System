using POS.Domain.Common;
using POS.Domain.Products;

namespace POS.Domain.Promotions;

public class PromotionTarget : BaseEntity
{
    public PromotionTarget() : base()
    {
    }

    public Guid PromotionId { get; private set; }
    public Promotion Promotion { get; private set; } = default!;

    public Guid? CategoryId { get; private set; }
    public Category? Category { get; private set; }

    public Guid? SkuId { get; private set; }
    public Sku? Sku { get; private set; }
}
