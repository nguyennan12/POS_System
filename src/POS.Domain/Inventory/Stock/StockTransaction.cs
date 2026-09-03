using POS.Domain.Common;
using POS.Domain.Employees;
using POS.Domain.Inventory.Enums;
using POS.Domain.Inventory.StockIn;
using POS.Domain.Orders;
using POS.Domain.Products;
using POS.Domain.Stores;

namespace POS.Domain.Inventory.Stock;

public class StockTransaction : BaseEntity
{
    public StockTransaction() : base()
    {
    }

  public Guid StoreId { get; private set; }
  public Store Store { get; private set; } = default!;

  public Guid SkuId { get; private set; }
  public Sku Sku { get; private set; } = default!;

  public StockTransactionType Type { get; private set; }
  public decimal Qty { get; private set; }
  public Guid? OrderId { get; private set; }
  public Order? Order { get; private set; }
  public Guid? StockInVoucherId { get; private set; }
  public StockInVoucher? StockInVoucher { get; private set; }
  public string? Note { get; private set; }
  public Guid CreatedBy { get; private set; }
  public Employee CreatedByEmployee { get; private set; } = default!;
  public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
}
