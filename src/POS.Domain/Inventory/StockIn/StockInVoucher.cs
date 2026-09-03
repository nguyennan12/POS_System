using POS.Domain.Common;
using POS.Domain.Employees;
using POS.Domain.Inventory.Enums;
using POS.Domain.Inventory.Suppliers;
using POS.Domain.Stores;

namespace POS.Domain.Inventory.StockIn;

public class StockInVoucher : BaseEntity
{
    public StockInVoucher() : base()
    {
    }

  public Guid StoreId { get; private set; }
  public Store Store { get; private set; } = default!;

  public Guid SupplierId { get; private set; }
  public Supplier Supplier { get; private set; } = default!;

  public decimal TotalAmount { get; private set; }
  public StockInVoucherStatus Status { get; private set; } = StockInVoucherStatus.Completed;
  public string? Note { get; private set; }
  public Guid CreatedBy { get; private set; }
  public Employee CreatedByEmployee { get; private set; } = default!;
  public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

  public ICollection<StockInVoucherItem> Items { get; private set; } = new List<StockInVoucherItem>();
}
