using POS.Domain.Common;

namespace POS.Domain.Promotions;

public class Voucher : BaseEntity
{
    public Voucher() : base()
    {
    }

    public Guid PromotionId { get; private set; }
    public Promotion Promotion { get; private set; } = default!;

    public string Code { get; private set; } = default!;
    public int MaxUses { get; private set; }
    public int UsedCount { get; private set; }
    public int PerCustomerLimit { get; private set; } = 1;
    public DateTime? ExpiresAt { get; private set; }
    public bool IsActive { get; private set; } = true;
}
