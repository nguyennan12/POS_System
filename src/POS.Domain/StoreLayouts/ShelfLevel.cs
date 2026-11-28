using POS.Domain.Common;

namespace POS.Domain.StoreLayouts;

public class ShelfLevel : BaseEntity
{
    public Guid ShelfId { get; private set; }
    public int LevelNumber { get; private set; }
    public string? Label { get; private set; }
    public int SortOrder { get; private set; }

    // Navigation
    public Shelf? Shelf { get; private set; }
    private readonly List<ProductPlacement> _placements = [];
    public IReadOnlyCollection<ProductPlacement> Placements => _placements.AsReadOnly();

    private ShelfLevel() { } // EF Core

    public ShelfLevel(
        Guid shelfId,
        int levelNumber,
        string? label,
        int sortOrder,
        Guid? id = null) : base(id)
    {
        ShelfId = shelfId;
        LevelNumber = levelNumber;
        Label = label;
        SortOrder = sortOrder;
    }

    public void Update(string? label, int sortOrder)
    {
        Label = label;
        SortOrder = sortOrder;
    }

    public void AddPlacement(ProductPlacement placement)
    {
        _placements.Add(placement);
    }

    public void RemovePlacement(ProductPlacement placement)
    {
        _placements.Remove(placement);
    }
}
