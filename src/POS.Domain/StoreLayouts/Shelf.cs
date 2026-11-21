using POS.Domain.Common;
using POS.Domain.StoreLayouts.Enums;

namespace POS.Domain.StoreLayouts;

public class Shelf : BaseEntity
{
    public Guid LayoutId { get; private set; }
    public Guid? ZoneId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public int PositionX { get; private set; }
    public int PositionY { get; private set; }
    public int Width { get; private set; }
    public int Height { get; private set; }
    public ShelfOrientation Orientation { get; private set; }
    public ShelfStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    // Navigation
    public StoreLayout? Layout { get; private set; }
    public StoreZone? Zone { get; private set; }
    private readonly List<ShelfLevel> _levels = [];
    public IReadOnlyCollection<ShelfLevel> Levels => _levels.AsReadOnly();

    private Shelf() { } // EF Core

    public Shelf(
        Guid layoutId,
        Guid? zoneId,
        string code,
        string name,
        int positionX,
        int positionY,
        int width,
        int height,
        ShelfOrientation orientation,
        DateTime now,
        ShelfStatus status = ShelfStatus.Active,
        Guid? id = null) : base(id)
    {
        LayoutId = layoutId;
        ZoneId = zoneId;
        Code = code;
        Name = name;
        PositionX = positionX;
        PositionY = positionY;
        Width = width;
        Height = height;
        Orientation = orientation;
        Status = status;
        CreatedAt = now;
        UpdatedAt = now;
    }

    public void Update(
        Guid? zoneId,
        string code,
        string name,
        int positionX,
        int positionY,
        int width,
        int height,
        ShelfOrientation orientation,
        ShelfStatus status,
        DateTime now)
    {
        ZoneId = zoneId;
        Code = code;
        Name = name;
        PositionX = positionX;
        PositionY = positionY;
        Width = width;
        Height = height;
        Orientation = orientation;
        Status = status;
        UpdatedAt = now;
    }

    public ShelfLevel AddLevel(int levelNumber, string? label, int sortOrder, Guid? levelId = null)
    {
        var level = new ShelfLevel(Id, levelNumber, label, sortOrder, levelId);
        _levels.Add(level);
        return level;
    }

    public bool RemoveLevel(Guid levelId)
    {
        var level = _levels.FirstOrDefault(l => l.Id == levelId);
        if (level == null) return false;
        return _levels.Remove(level);
    }
}
