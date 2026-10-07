using POS.Domain.Common;
using POS.Domain.Customers.Enums;
using POS.Domain.Orders;

namespace POS.Domain.Customers;

public class PointTransaction : BaseEntity
{
  public PointTransaction() : base()
  {
  }


  /// Creates a point transaction with the supplied amount, type, and optional references, timestamped in UTC.
  /// </summary>
  public PointTransaction(
      Guid customerId,
      decimal points,
      PointTransactionType type,
      Guid? orderId = null,
      string? note = null,
      Guid? id = null) : base(id)
  {
    CustomerId = customerId;
    Points = points;
    Type = type;
    OrderId = orderId;
    Note = note;
    CreatedAt = DateTime.UtcNow;
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
