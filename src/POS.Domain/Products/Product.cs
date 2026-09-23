using POS.Domain.Common;
using POS.Domain.Products.Enums;
using POS.Domain.Stores;

namespace POS.Domain.Products;

public class Product : BaseEntity
{
    public Product() : base()
    {
    }

    public Product(
        Guid storeId,
        Guid categoryId,
        string name,
        string baseUnit,
        string? description = null,
        string? brand = null,
        string? imageUrl = null,
        ProductStatus status = ProductStatus.Active,
        Guid? id = null) : base(id)
    {
        StoreId = storeId;
        CategoryId = categoryId;
        Name = name;
        BaseUnit = baseUnit;
        Description = description;
        Brand = brand;
        ImageUrl = imageUrl;
        Status = status;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public Guid StoreId { get; private set; }
    public Store Store { get; private set; } = default!;

    public Guid CategoryId { get; private set; }
    public Category Category { get; private set; } = default!;

    public string Name { get; private set; } = default!;
    public string? Description { get; private set; }
    public string? Brand { get; private set; }
    public string BaseUnit { get; private set; } = default!;
    public string? ImageUrl { get; private set; }
    public ProductStatus Status { get; private set; } = ProductStatus.Active;
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; private set; } = DateTime.UtcNow;
}
