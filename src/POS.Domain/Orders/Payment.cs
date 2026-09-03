using POS.Domain.Common;
using POS.Domain.Orders.Enums;

namespace POS.Domain.Orders;

public class Payment : BaseEntity
{
    public Payment() : base()
    {
    }

    public Guid OrderId { get; private set; }
    public Order Order { get; private set; } = default!;

    public PaymentMethod Method { get; private set; }
    public decimal Amount { get; private set; }
    public decimal? ChangeAmount { get; private set; }
    public string? TransactionRef { get; private set; }
    public PaymentStatus Status { get; private set; } = PaymentStatus.Pending;
    public string? GatewayResponseJson { get; private set; }
    public DateTime? PaidAt { get; private set; }
}
