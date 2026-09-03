using POS.Domain.Common;
using POS.Domain.Stores;

namespace POS.Domain.Products;

public class Category : BaseEntity
{
    public Category() : base()
    {
    }

    public Guid StoreId { get; private set; }
    public Store Store { get; private set; } = default!;

    public Guid? ParentId { get; private set; }
    public Category? Parent { get; private set; }

    public string Name { get; private set; } = default!;
    public int DisplayOrder { get; private set; }
    public string? ImageUrl { get; private set; }
    public bool IsVisible { get; private set; } = true;
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
}
