using POS.Domain.Common;
using POS.Domain.Customers;
using POS.Domain.Orders;

namespace POS.Domain.Promotions;

public class VoucherUsage : BaseEntity
{
    public VoucherUsage() : base()
    {
    }

    public Guid VoucherId { get; private set; }
    public Voucher Voucher { get; private set; } = default!;

    public Guid CustomerId { get; private set; }
    public Customer Customer { get; private set; } = default!;

    public Guid OrderId { get; private set; }
    public Order Order { get; private set; } = default!;
    public DateTime UsedAt { get; private set; } = DateTime.UtcNow;
}
