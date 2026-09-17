using POS.Domain.Common;
using POS.Domain.Products;

namespace POS.Domain.Promotions;

public class PromotionTarget : BaseEntity
{
    public PromotionTarget() : base()
    {
    }

    /// <summary>Creates a target that associates a promotion with a category or SKU.</summary>
    public PromotionTarget(Guid promotionId, Guid? categoryId = null, Guid? skuId = null, Guid? id = null) : base(id)
    {
        PromotionId = promotionId;
        CategoryId = categoryId;
        SkuId = skuId;
    }

    /// <summary>Creates a category target for a promotion.</summary>
    public static PromotionTarget ForCategory(Guid promotionId, Guid categoryId) =>
        new(promotionId, categoryId: categoryId);

    /// <summary>Creates a SKU target for a promotion.</summary>
    public static PromotionTarget ForSku(Guid promotionId, Guid skuId) =>
        new(promotionId, skuId: skuId);

    public Guid PromotionId { get; private set; }
    public Promotion Promotion { get; private set; } = default!;

    public Guid? CategoryId { get; private set; }
    public Category? Category { get; private set; }

    public Guid? SkuId { get; private set; }
    public Sku? Sku { get; private set; }
}
