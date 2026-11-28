using POS.Domain.Common;

namespace POS.Domain.StoreLayouts;

public class ProductPlacement : BaseEntity
{
    public Guid ShelfLevelId { get; private set; }
    public Guid SkuId { get; private set; }
    public bool IsPrimary { get; private set; }
    public string? Note { get; private set; }
    public Guid UpdatedBy { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    // Navigation
    public ShelfLevel? ShelfLevel { get; private set; }

    private ProductPlacement() { } // EF Core

    public ProductPlacement(
        Guid shelfLevelId,
        Guid skuId,
        bool isPrimary,
        string? note,
        Guid actorId,
        DateTime now,
        Guid? id = null) : base(id)
    {
        ShelfLevelId = shelfLevelId;
        SkuId = skuId;
        IsPrimary = isPrimary;
        Note = note;
        UpdatedBy = actorId;
        CreatedAt = now;
        UpdatedAt = now;
    }

    public void Move(Guid targetShelfLevelId, string? note, Guid actorId, DateTime now)
    {
        ShelfLevelId = targetShelfLevelId;
        if (note != null) Note = note;
        UpdatedBy = actorId;
        UpdatedAt = now;
    }

    public void SetPrimary(bool isPrimary, Guid actorId, DateTime now)
    {
        IsPrimary = isPrimary;
        UpdatedBy = actorId;
        UpdatedAt = now;
    }

    public void UpdateNote(string? note, Guid actorId, DateTime now)
    {
        Note = note;
        UpdatedBy = actorId;
        UpdatedAt = now;
    }
}
