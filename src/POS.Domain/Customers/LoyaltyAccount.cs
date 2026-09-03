using POS.Domain.Common;

namespace POS.Domain.Customers;

public class LoyaltyAccount : BaseEntity
{
    public LoyaltyAccount() : base()
    {
    }

    public Guid CustomerId { get; private set; }
    public Customer Customer { get; private set; } = default!;
    public decimal PointsBalance { get; private set; }
    public DateTime LastUpdated { get; private set; } = DateTime.UtcNow;
}
