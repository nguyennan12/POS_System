using POS.Domain.Common;
using POS.Domain.Products;

namespace POS.Domain.Inventory.StockIn;

public class StockInVoucherItem : BaseEntity
{
    public StockInVoucherItem() : base()
    {
    }

    public StockInVoucherItem(Guid voucherId, Guid skuId, decimal qty, decimal unitPrice) : base()
    {
        VoucherId = voucherId;
        SkuId = skuId;
        Qty = qty;
        UnitPrice = unitPrice;
        TotalPrice = qty * unitPrice;
    }

    public Guid VoucherId { get; private set; }
    public StockInVoucher Voucher { get; private set; } = default!;

    public Guid SkuId { get; private set; }
    public Sku Sku { get; private set; } = default!;

    public decimal Qty { get; private set; }
    public decimal UnitPrice { get; private set; }
    public decimal TotalPrice { get; private set; }
}

