using POS.Domain.Common;

namespace POS.Domain.StoreLayouts;

public class StoreZone : BaseEntity
{
    public Guid LayoutId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string Color { get; private set; } = "#3B82F6";
    public int PositionX { get; private set; }
    public int PositionY { get; private set; }
    public int Width { get; private set; }
    public int Height { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    // Navigation
    public StoreLayout? Layout { get; private set; }

    private StoreZone() { } // EF Core

    public StoreZone(
        Guid layoutId,
        string code,
        string name,
        string color,
        int positionX,
        int positionY,
        int width,
        int height,
        DateTime now,
        Guid? id = null) : base(id)
    {
        LayoutId = layoutId;
        Code = code;
        Name = name;
        Color = color;
        PositionX = positionX;
        PositionY = positionY;
        Width = width;
        Height = height;
        CreatedAt = now;
        UpdatedAt = now;
    }

    public void Update(
        string code,
        string name,
        string color,
        int positionX,
        int positionY,
        int width,
        int height,
        DateTime now)
    {
        Code = code;
        Name = name;
        Color = color;
        PositionX = positionX;
        PositionY = positionY;
        Width = width;
        Height = height;
        UpdatedAt = now;
    }
}
