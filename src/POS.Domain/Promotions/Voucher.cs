using POS.Domain.Common;

namespace POS.Domain.Promotions;

public class Voucher : BaseEntity
{
    public Voucher() : base()
    {
    }

    public Voucher(
        Guid promotionId,
        string code,
        int maxUses,
        int perCustomerLimit = 1,
        DateTime? expiresAt = null,
        bool isActive = true,
        Guid? id = null) : base(id)
    {
        PromotionId = promotionId;
        Code = code;
        MaxUses = maxUses;
        PerCustomerLimit = perCustomerLimit;
        ExpiresAt = expiresAt;
        IsActive = isActive;
        UsedCount = 0;
    }

    public Guid PromotionId { get; private set; }
    public Promotion Promotion { get; private set; } = default!;

    public string Code { get; private set; } = default!;
    public int MaxUses { get; private set; }
    public int UsedCount { get; private set; }
    public int PerCustomerLimit { get; private set; } = 1;
    public DateTime? ExpiresAt { get; private set; }
    public bool IsActive { get; private set; } = true;

    public bool CanBeUsed(DateTime now, int customerUsedCount = 0)
    {
        if (!IsActive)
            return false;

        if (ExpiresAt.HasValue && now > ExpiresAt.Value)
            return false;

        if (UsedCount >= MaxUses)
            return false;

        if (customerUsedCount >= PerCustomerLimit)
            return false;

        return true;
    }

    public void RecordUse()
    {
        UsedCount++;
    }
}
