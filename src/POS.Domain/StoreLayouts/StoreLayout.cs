using POS.Domain.Common;
using POS.Domain.StoreLayouts.Enums;
using POS.Domain.StoreLayouts.Errors;

namespace POS.Domain.StoreLayouts;

public class StoreLayout : BaseEntity
{
    public Guid StoreId { get; private set; }
    public int VersionNumber { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public int GridWidth { get; private set; }
    public int GridHeight { get; private set; }
    public StoreLayoutStatus Status { get; private set; }
    public int Revision { get; private set; }
    public Guid CreatedBy { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public Guid? PublishedBy { get; private set; }
    public DateTime? PublishedAt { get; private set; }

    // Navigation collections
    private readonly List<StoreZone> _zones = [];
    public IReadOnlyCollection<StoreZone> Zones => _zones.AsReadOnly();

    private readonly List<StoreLayoutCell> _cells = [];
    public IReadOnlyCollection<StoreLayoutCell> Cells => _cells.AsReadOnly();

    private readonly List<Shelf> _shelves = [];
    public IReadOnlyCollection<Shelf> Shelves => _shelves.AsReadOnly();

    private StoreLayout() { } // EF Core

    public static Result<StoreLayout> Create(
        Guid storeId,
        int versionNumber,
        string name,
        int gridWidth,
        int gridHeight,
        Guid createdBy,
        DateTime now,
        Guid? id = null)
    {
        if (gridWidth < 4 || gridWidth > 100 || gridHeight < 4 || gridHeight > 100)
        {
            return Result<StoreLayout>.Failure(StoreLayoutErrors.InvalidGrid);
        }

        var layoutId = id ?? Guid.NewGuid();
        var layout = new StoreLayout
        {
            StoreId = storeId,
            VersionNumber = versionNumber,
            Name = name,
            GridWidth = gridWidth,
            GridHeight = gridHeight,
            Status = StoreLayoutStatus.Draft,
            Revision = 1,
            CreatedBy = createdBy,
            CreatedAt = now,
            UpdatedAt = now
        };
        typeof(BaseEntity).GetProperty(nameof(Id))?.SetValue(layout, layoutId);

        // Populate default cells (GridWidth x GridHeight) as Walkway
        for (int y = 0; y < gridHeight; y++)
        {
            for (int x = 0; x < gridWidth; x++)
            {
                layout._cells.Add(new StoreLayoutCell(layoutId, x, y, LayoutCellType.Walkway, 1));
            }
        }

        return Result<StoreLayout>.Success(layout);
    }

    private Result VerifyDraftAndRevision(int expectedRevision)
    {
        if (Status != StoreLayoutStatus.Draft)
            return Result.Failure(StoreLayoutErrors.NotDraft);

        if (Revision != expectedRevision)
            return Result.Failure(StoreLayoutErrors.RevisionConflict);

        return Result.Success();
    }

    public Result Rename(string name, int expectedRevision, DateTime now)
    {
        var check = VerifyDraftAndRevision(expectedRevision);
        if (check.IsFailure) return check;

        Name = name;
        Revision++;
        UpdatedAt = now;
        return Result.Success();
    }

    public Result AddZone(StoreZone zone, int expectedRevision, DateTime now)
    {
        var check = VerifyDraftAndRevision(expectedRevision);
        if (check.IsFailure) return check;

        if (_zones.Any(z => z.Code.Equals(zone.Code, StringComparison.OrdinalIgnoreCase)))
            return Result.Failure(new Error(ErrorType.AlreadyExists, "ZONE_CODE_DUPLICATE", $"Mã khu vực '{zone.Code}' đã tồn tại."));

        _zones.Add(zone);
        Revision++;
        UpdatedAt = now;
        return Result.Success();
    }

    public Result UpdateZone(
        Guid zoneId,
        string code,
        string name,
        string color,
        int x,
        int y,
        int width,
        int height,
        int expectedRevision,
        DateTime now)
    {
        var check = VerifyDraftAndRevision(expectedRevision);
        if (check.IsFailure) return check;

        var zone = _zones.FirstOrDefault(z => z.Id == zoneId);
        if (zone == null) return Result.Failure(StoreLayoutErrors.ZoneNotFound);

        if (_zones.Any(z => z.Id != zoneId && z.Code.Equals(code, StringComparison.OrdinalIgnoreCase)))
            return Result.Failure(new Error(ErrorType.AlreadyExists, "ZONE_CODE_DUPLICATE", $"Mã khu vực '{code}' đã tồn tại."));

        zone.Update(code, name, color, x, y, width, height, now);
        Revision++;
        UpdatedAt = now;
        return Result.Success();
    }

    public Result RemoveZone(Guid zoneId, int expectedRevision, DateTime now)
    {
        var check = VerifyDraftAndRevision(expectedRevision);
        if (check.IsFailure) return check;

        var zone = _zones.FirstOrDefault(z => z.Id == zoneId);
        if (zone == null) return Result.Failure(StoreLayoutErrors.ZoneNotFound);

        // Unlink zone from shelves belonging to this zone
        foreach (var shelf in _shelves.Where(s => s.ZoneId == zoneId))
        {
            shelf.Update(null, shelf.Code, shelf.Name, shelf.PositionX, shelf.PositionY, shelf.Width, shelf.Height, shelf.Orientation, shelf.Status, now);
        }

        _zones.Remove(zone);
        Revision++;
        UpdatedAt = now;
        return Result.Success();
    }

    public Result UpdateCells(
        IReadOnlyCollection<(int X, int Y, LayoutCellType CellType, int TraversalCost)> changes,
        int expectedRevision,
        DateTime now)
    {
        var check = VerifyDraftAndRevision(expectedRevision);
        if (check.IsFailure) return check;

        foreach (var (x, y, cellType, traversalCost) in changes)
        {
            if (x < 0 || x >= GridWidth || y < 0 || y >= GridHeight)
                return Result.Failure(StoreLayoutErrors.InvalidGrid);

            var cell = _cells.FirstOrDefault(c => c.X == x && c.Y == y);
            if (cell != null)
            {
                cell.Update(cellType, traversalCost);
            }
            else
            {
                _cells.Add(new StoreLayoutCell(Id, x, y, cellType, traversalCost));
            }
        }

        Revision++;
        UpdatedAt = now;
        return Result.Success();
    }

    public Result AddShelf(Shelf shelf, int expectedRevision, DateTime now)
    {
        var check = VerifyDraftAndRevision(expectedRevision);
        if (check.IsFailure) return check;

        if (shelf.PositionX < 0 || shelf.PositionY < 0 ||
            shelf.PositionX + shelf.Width > GridWidth ||
            shelf.PositionY + shelf.Height > GridHeight)
        {
            return Result.Failure(StoreLayoutErrors.ShelfOutOfBounds);
        }

        if (_shelves.Any(s => s.Code.Equals(shelf.Code, StringComparison.OrdinalIgnoreCase)))
            return Result.Failure(new Error(ErrorType.AlreadyExists, "SHELF_CODE_DUPLICATE", $"Mã kệ '{shelf.Code}' đã tồn tại."));

        // Check shelf overlap
        if (IsOverlapping(shelf.PositionX, shelf.PositionY, shelf.Width, shelf.Height, null))
            return Result.Failure(StoreLayoutErrors.ShelfOverlap);

        // Check if shelf footprint sits on blocked/entrance/checkout cell
        if (IsFootprintOnBlockedCell(shelf.PositionX, shelf.PositionY, shelf.Width, shelf.Height))
            return Result.Failure(StoreLayoutErrors.ShelfCellBlocked);

        _shelves.Add(shelf);
        Revision++;
        UpdatedAt = now;
        return Result.Success();
    }

    public Result UpdateShelf(
        Guid shelfId,
        Guid? zoneId,
        string code,
        string name,
        int x,
        int y,
        int width,
        int height,
        ShelfOrientation orientation,
        ShelfStatus status,
        int expectedRevision,
        DateTime now)
    {
        var check = VerifyDraftAndRevision(expectedRevision);
        if (check.IsFailure) return check;

        var shelf = _shelves.FirstOrDefault(s => s.Id == shelfId);
        if (shelf == null) return Result.Failure(StoreLayoutErrors.ShelfNotFound);

        if (x < 0 || y < 0 || x + width > GridWidth || y + height > GridHeight)
            return Result.Failure(StoreLayoutErrors.ShelfOutOfBounds);

        if (_shelves.Any(s => s.Id != shelfId && s.Code.Equals(code, StringComparison.OrdinalIgnoreCase)))
            return Result.Failure(new Error(ErrorType.AlreadyExists, "SHELF_CODE_DUPLICATE", $"Mã kệ '{code}' đã tồn tại."));

        if (IsOverlapping(x, y, width, height, shelfId))
            return Result.Failure(StoreLayoutErrors.ShelfOverlap);

        if (IsFootprintOnBlockedCell(x, y, width, height))
            return Result.Failure(StoreLayoutErrors.ShelfCellBlocked);

        shelf.Update(zoneId, code, name, x, y, width, height, orientation, status, now);
        Revision++;
        UpdatedAt = now;
        return Result.Success();
    }

    public Result RemoveShelf(Guid shelfId, int expectedRevision, DateTime now)
    {
        var check = VerifyDraftAndRevision(expectedRevision);
        if (check.IsFailure) return check;

        var shelf = _shelves.FirstOrDefault(s => s.Id == shelfId);
        if (shelf == null) return Result.Failure(StoreLayoutErrors.ShelfNotFound);

        // Check if any level in shelf has placements
        if (shelf.Levels.Any(l => l.Placements.Count > 0))
            return Result.Failure(StoreLayoutErrors.ShelfHasPlacements);

        _shelves.Remove(shelf);
        Revision++;
        UpdatedAt = now;
        return Result.Success();
    }

    public Result Publish(Guid actorId, int expectedRevision, DateTime now)
    {
        var check = VerifyDraftAndRevision(expectedRevision);
        if (check.IsFailure) return check;

        // Must have at least one entrance cell
        if (!_cells.Any(c => c.CellType == LayoutCellType.Entrance))
            return Result.Failure(StoreLayoutErrors.EntranceRequired);

        Status = StoreLayoutStatus.Active;
        PublishedBy = actorId;
        PublishedAt = now;
        Revision++;
        UpdatedAt = now;
        return Result.Success();
    }

    public Result Archive(Guid actorId, DateTime now)
    {
        Status = StoreLayoutStatus.Archived;
        Revision++;
        UpdatedAt = now;
        return Result.Success();
    }

    private bool IsOverlapping(int x, int y, int width, int height, Guid? ignoreShelfId)
    {
        return _shelves.Any(s =>
            s.Id != ignoreShelfId &&
            s.PositionX < x + width &&
            s.PositionX + s.Width > x &&
            s.PositionY < y + height &&
            s.PositionY + s.Height > y);
    }

    private bool IsFootprintOnBlockedCell(int x, int y, int width, int height)
    {
        return _cells.Any(c =>
            c.X >= x && c.X < x + width &&
            c.Y >= y && c.Y < y + height &&
            (c.CellType == LayoutCellType.Entrance || c.CellType == LayoutCellType.Checkout || c.CellType == LayoutCellType.Blocked));
    }
}
