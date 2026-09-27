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

    private StockTransaction(
        Guid storeId,
        Guid skuId,
        StockTransactionType type,
        decimal qty,
        Guid createdBy,
        Guid? orderId = null,
        string? note = null,
        Guid? id = null) : base(id)
    {
        StoreId = storeId;
        SkuId = skuId;
        Type = type;
        Qty = qty;
        CreatedBy = createdBy;
        OrderId = orderId;
        Note = note;
        CreatedAt = DateTime.UtcNow;
    }

    public static StockTransaction CreateSaleOut(
        Guid storeId,
        Guid skuId,
        decimal qty,
        Guid createdBy,
        Guid orderId,
        string? note = null)
    {
        if (qty <= 0)
            throw new ArgumentOutOfRangeException(nameof(qty), "Số lượng xuất kho phải lớn hơn 0.");
        return new StockTransaction(storeId, skuId, StockTransactionType.SaleOut, qty, createdBy, orderId, note);
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
