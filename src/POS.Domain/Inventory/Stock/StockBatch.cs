using POS.Domain.Common;
using POS.Domain.Products;
using POS.Domain.Stores;

namespace POS.Domain.Inventory.Stock;

public class StockBatch : BaseEntity
{
    public StockBatch() : base()
    {
    }

  public Guid StoreId { get; private set; }
  public Store Store { get; private set; } = default!;

  public Guid SkuId { get; private set; }
  public Sku Sku { get; private set; } = default!;

  public string BatchNo { get; private set; } = default!;
  public decimal Qty { get; private set; }
  public DateOnly? ExpiryDate { get; private set; }
  public DateTime ReceivedAt { get; private set; } = DateTime.UtcNow;
}
