using POS.Domain.Common;
using POS.Domain.Products;

namespace POS.Domain.Promotions;

public class PromotionTarget : BaseEntity
{
    public PromotionTarget() : base()
    {
    }

    public PromotionTarget(Guid promotionId, Guid? categoryId = null, Guid? skuId = null, Guid? id = null) : base(id)
    {
        PromotionId = promotionId;
        CategoryId = categoryId;
        SkuId = skuId;
    }

    public static PromotionTarget ForCategory(Guid promotionId, Guid categoryId) =>
        new(promotionId, categoryId: categoryId);

    public static PromotionTarget ForSku(Guid promotionId, Guid skuId) =>
        new(promotionId, skuId: skuId);

    public Guid PromotionId { get; private set; }
    public Promotion Promotion { get; private set; } = default!;

    public Guid? CategoryId { get; private set; }
    public Category? Category { get; private set; }

    public Guid? SkuId { get; private set; }
    public Sku? Sku { get; private set; }
}
