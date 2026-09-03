using POS.Domain.Common;

namespace POS.Domain.Products;

public class UnitConversion : BaseEntity
{
    public UnitConversion() : base()
    {
    }

    public Guid SkuId { get; private set; }
    public Sku Sku { get; private set; } = default!;

    public string UnitName { get; private set; } = default!;
    public decimal ConversionFactor { get; private set; }
    public decimal SellPrice { get; private set; }
}
