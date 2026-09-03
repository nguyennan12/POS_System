using POS.Domain.Common;
using POS.Domain.Customers;
using POS.Domain.Employees;
using POS.Domain.Orders.Enums;
using POS.Domain.Stores;

namespace POS.Domain.Orders;

public class Order : BaseEntity
{
    public Order() : base()
    {
    }

    public Guid StoreId { get; private set; }
    public Store Store { get; private set; } = default!;

    public Guid ShiftId { get; private set; }
    public Shift Shift { get; private set; } = default!;

    public Guid? CustomerId { get; private set; }
    public Customer? Customer { get; private set; }

    public OrderStatus Status { get; private set; } = OrderStatus.Draft;
    public string CurrencyCode { get; private set; } = "VND";
    public decimal Subtotal { get; private set; }
    public decimal DiscountTotal { get; private set; }
    public decimal TaxTotal { get; private set; }
    public decimal GrandTotal { get; private set; }
    public string? Note { get; private set; }
    public Guid CreatedBy { get; private set; }
    public Employee CreatedByEmployee { get; private set; } = default!;
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime? PaidAt { get; private set; }

    public ICollection<OrderItem> Items { get; private set; } = new List<OrderItem>();
}
