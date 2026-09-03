using POS.Domain.Common;
using POS.Domain.Customers.Enums;
using POS.Domain.Orders;

namespace POS.Domain.Customers;

public class PointTransaction : BaseEntity
{
    public PointTransaction() : base()
    {
    }

    public Guid CustomerId { get; private set; }
    public Customer Customer { get; private set; } = default!;
    public decimal Points { get; private set; }
    public PointTransactionType Type { get; private set; }
    public Guid? OrderId { get; private set; }
    public Order? Order { get; private set; }
    public string? Note { get; private set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
}
