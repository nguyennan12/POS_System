using POS.Domain.Common;
using POS.Domain.Customers.Enums;

namespace POS.Domain.Customers;

public class MemberTier : BaseEntity
{
    public MemberTier() : base()
    {
    }

    public MemberTier(
        MemberTierName name,
        decimal minSpending,
        decimal pointRate,
        decimal discountRate,
        string? displayColor = null,
        Guid? id = null) : base(id)
    {
        Name = name;
        MinSpending = minSpending;
        PointRate = pointRate;
        DiscountRate = discountRate;
        DisplayColor = displayColor;
    }

    public MemberTierName Name { get; private set; }
    public decimal MinSpending { get; private set; }
    public decimal PointRate { get; private set; }
    public decimal DiscountRate { get; private set; }
    public string? DisplayColor { get; private set; }

    public void Update(decimal minSpending, decimal pointRate, decimal discountRate, string? displayColor)
    {
        MinSpending = minSpending;
        PointRate = pointRate;
        DiscountRate = discountRate;
        DisplayColor = displayColor;
    }
}
