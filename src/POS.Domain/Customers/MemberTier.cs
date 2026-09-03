using POS.Domain.Common;
using POS.Domain.Customers.Enums;

namespace POS.Domain.Customers;

public class MemberTier : BaseEntity
{
    public MemberTier() : base()
    {
    }

    public MemberTierName Name { get; private set; }
    public decimal MinSpending { get; private set; }
    public decimal PointRate { get; private set; }
    public decimal DiscountRate { get; private set; }
    public string? DisplayColor { get; private set; }
}
