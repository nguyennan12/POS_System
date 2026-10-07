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
        decimal pointRedemptionRate = 1000m,
        Guid? id = null) : base(id)
    {
        Name = name;
        MinSpending = minSpending;
        PointRate = pointRate;
        DiscountRate = discountRate;
        DisplayColor = displayColor;
        PointRedemptionRate = pointRedemptionRate;
    }

    public MemberTierName Name { get; private set; }
    public decimal MinSpending { get; private set; }
    public decimal PointRate { get; private set; }
    public decimal DiscountRate { get; private set; }
    public string? DisplayColor { get; private set; }
    public decimal PointRedemptionRate { get; private set; } = 1000m;

    public void Update(
        decimal minSpending,
        decimal pointRate,
        decimal discountRate,
        string? displayColor,
        decimal? pointRedemptionRate = null)
    {
        MinSpending = minSpending;
        PointRate = pointRate;
        DiscountRate = discountRate;
        DisplayColor = displayColor;
        if (pointRedemptionRate.HasValue && pointRedemptionRate.Value > 0)
        {
            PointRedemptionRate = pointRedemptionRate.Value;
        }
    }

    /// <summary>Calculates the required points to pay for a specified currency amount, rounding up (Ceiling).</summary>
    public decimal CalculateRequiredPoints(decimal currencyAmount)
    {
        if (currencyAmount <= 0) return 0m;
        if (PointRedemptionRate <= 0) return 0m;
        return Math.Ceiling(currencyAmount / PointRedemptionRate);
    }

    /// <summary>Converts loyalty points to currency value.</summary>
    public decimal ConvertPointsToCurrency(decimal points)
    {
        if (points <= 0) return 0m;
        return points * PointRedemptionRate;
    }
}
