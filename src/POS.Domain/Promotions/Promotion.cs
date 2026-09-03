using POS.Domain.Common;
using POS.Domain.Employees;
using POS.Domain.Promotions.Enums;
using POS.Domain.Stores;

namespace POS.Domain.Promotions;

public class Promotion : BaseEntity
{
    public Promotion() : base()
    {
    }

    public Guid StoreId { get; private set; }
    public Store Store { get; private set; } = default!;

    public string Name { get; private set; } = default!;
    public PromotionType Type { get; private set; }
    public decimal Value { get; private set; }
    public decimal MinOrderAmount { get; private set; }
    public decimal? MaxDiscountAmount { get; private set; }
    public string? ConditionsJson { get; private set; }
    public int Priority { get; private set; }
    public bool IsStackable { get; private set; }
    public bool IsExclusive { get; private set; }
    public PromotionAppliesTo AppliesTo { get; private set; }
    public DateTime ValidFrom { get; private set; }
    public DateTime? ValidTo { get; private set; }
    public PromotionStatus Status { get; private set; } = PromotionStatus.Active;
    public Guid CreatedBy { get; private set; }
    public Employee CreatedByEmployee { get; private set; } = default!;
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
}
