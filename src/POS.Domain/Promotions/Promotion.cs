using POS.Domain.Common;
using POS.Domain.Employees;
using POS.Domain.Promotions.Enums;
using POS.Domain.Stores;

namespace POS.Domain.Promotions;

public class Promotion : BaseEntity
{
    private readonly List<PromotionTarget> _targets = [];
    public IReadOnlyCollection<PromotionTarget> Targets => _targets.AsReadOnly();

    public Promotion() : base()
    {
    }

    public Promotion(
        Guid storeId,
        string name,
        PromotionType type,
        decimal value,
        decimal minOrderAmount = 0,
        decimal? maxDiscountAmount = null,
        string? conditionsJson = null,
        int priority = 0,
        bool isStackable = false,
        bool isExclusive = false,
        PromotionAppliesTo appliesTo = PromotionAppliesTo.All,
        DateTime? validFrom = null,
        DateTime? validTo = null,
        PromotionStatus status = PromotionStatus.Active,
        Guid? createdBy = null,
        Guid? id = null) : base(id)
    {
        StoreId = storeId;
        Name = name;
        Type = type;
        Value = value;
        MinOrderAmount = minOrderAmount;
        MaxDiscountAmount = maxDiscountAmount;
        ConditionsJson = conditionsJson;
        Priority = priority;
        IsStackable = isStackable;
        IsExclusive = isExclusive;
        AppliesTo = appliesTo;
        ValidFrom = validFrom ?? DateTime.UtcNow;
        ValidTo = validTo;
        Status = status;
        CreatedBy = createdBy ?? Guid.Empty;
        CreatedAt = DateTime.UtcNow;
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

    public void AddTarget(PromotionTarget target)
    {
        _targets.Add(target);
    }

    public void AddTargetSku(Guid skuId)
    {
        _targets.Add(PromotionTarget.ForSku(Id, skuId));
    }

    public void AddTargetCategory(Guid categoryId)
    {
        _targets.Add(PromotionTarget.ForCategory(Id, categoryId));
    }

    public bool IsActiveAt(DateTime now)
    {
        if (Status != PromotionStatus.Active)
            return false;

        if (now < ValidFrom)
            return false;

        if (ValidTo.HasValue && now > ValidTo.Value)
            return false;

        return true;
    }

    public void Deactivate()
    {
        Status = PromotionStatus.Inactive;
    }

    public void Activate()
    {
        Status = PromotionStatus.Active;
    }
}
