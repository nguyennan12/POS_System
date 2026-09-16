using POS.Domain.Common;
using POS.Domain.Stores;

namespace POS.Domain.Products;

public class Category : BaseEntity
{
    private Category() : base()
    {
    }

    private Category(Guid storeId, string name, Guid? parentId, int displayOrder, string? imageUrl, bool isVisible) : base()
    {
        StoreId = storeId;
        Name = name;
        ParentId = parentId;
        DisplayOrder = displayOrder;
        ImageUrl = imageUrl;
        IsVisible = isVisible;
    }

    public static Category Create(Guid storeId, string name, Guid? parentId = null, int displayOrder = 0, string? imageUrl = null, bool isVisible = true)
    {
        return new Category(storeId, name, parentId, displayOrder, imageUrl, isVisible);
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

    public void Update(string name, Guid? parentId, int displayOrder, string? imageUrl, bool isVisible)
    {
        Name = name;
        ParentId = parentId;
        DisplayOrder = displayOrder;
        ImageUrl = imageUrl;
        IsVisible = isVisible;
    }
}
