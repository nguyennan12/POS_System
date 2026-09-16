using POS.Domain.Common;

namespace POS.Domain.Customers;

public class LoyaltyAccount : BaseEntity
{
    public LoyaltyAccount() : base()
    {
    }

    public LoyaltyAccount(Guid customerId, decimal pointsBalance = 0, Guid? id = null) : base(id)
    {
        CustomerId = customerId;
        PointsBalance = pointsBalance;
        LastUpdated = DateTime.UtcNow;
    }

    public Guid CustomerId { get; private set; }
    public Customer Customer { get; private set; } = default!;
    public decimal PointsBalance { get; private set; }
    public DateTime LastUpdated { get; private set; } = DateTime.UtcNow;

    public void AddPoints(decimal points)
    {
        if (points < 0) throw new ArgumentOutOfRangeException(nameof(points), "Points to add must be non-negative.");
        PointsBalance += points;
        LastUpdated = DateTime.UtcNow;
    }

    public bool DeductPoints(decimal points)
    {
        if (points < 0 || PointsBalance < points) return false;
        PointsBalance -= points;
        LastUpdated = DateTime.UtcNow;
        return true;
    }

    public void SetPoints(decimal newBalance)
    {
        if (newBalance < 0) throw new ArgumentOutOfRangeException(nameof(newBalance), "Points balance cannot be negative.");
        PointsBalance = newBalance;
        LastUpdated = DateTime.UtcNow;
    }
}
