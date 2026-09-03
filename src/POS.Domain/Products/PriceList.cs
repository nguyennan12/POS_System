using POS.Domain.Common;
using POS.Domain.Employees;
using POS.Domain.Stores;

namespace POS.Domain.Products;

public class PriceList : BaseEntity
{
    public PriceList() : base()
    {
    }

    public Guid StoreId { get; private set; }
    public Store Store { get; private set; } = default!;

    public Guid SkuId { get; private set; }
    public Sku Sku { get; private set; } = default!;

    public decimal Price { get; private set; }
    public DateTime ValidFrom { get; private set; }
    public DateTime? ValidTo { get; private set; }
    public string? CustomerGroup { get; private set; }
    public Guid CreatedBy { get; private set; }
    public Employee CreatedByEmployee { get; private set; } = default!;
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
}
