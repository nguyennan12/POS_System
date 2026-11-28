namespace POS.Contracts.V1.StoreLayouts;

public sealed record CreateStoreLayoutRequest(
    string Name,
    int GridWidth,
    int GridHeight,
    Guid? StoreId = null);

public sealed record UpdateStoreLayoutRequest(
    string Name,
    int ExpectedRevision);

public sealed record LayoutCellChangeRequest(
    int X,
    int Y,
    string CellType,
    int TraversalCost = 1);

public sealed record UpdateLayoutCellsRequest(
    IReadOnlyList<LayoutCellChangeRequest> Cells,
    int ExpectedRevision);

public sealed record CreateZoneRequest(
    string Code,
    string Name,
    string Color,
    int PositionX,
    int PositionY,
    int Width,
    int Height,
    int ExpectedRevision);

public sealed record UpdateZoneRequest(
    string Code,
    string Name,
    string Color,
    int PositionX,
    int PositionY,
    int Width,
    int Height,
    int ExpectedRevision);

public sealed record CreateShelfRequest(
    Guid? ZoneId,
    string Code,
    string Name,
    int PositionX,
    int PositionY,
    int Width,
    int Height,
    string Orientation,
    int ExpectedRevision);

public sealed record UpdateShelfRequest(
    Guid? ZoneId,
    string Code,
    string Name,
    int PositionX,
    int PositionY,
    int Width,
    int Height,
    string Orientation,
    string Status,
    int ExpectedRevision);

public sealed record AddShelfLevelRequest(
    int LevelNumber,
    string? Label,
    int SortOrder,
    int ExpectedRevision);

public sealed record UpdateShelfLevelRequest(
    string? Label,
    int SortOrder,
    int ExpectedRevision);

public sealed record CloneStoreLayoutRequest(
    Guid? TargetStoreId,
    string NewName,
    bool CopyPlacements,
    int ExpectedRevision);

public sealed record PublishStoreLayoutRequest(
    int ExpectedRevision);

public sealed record ArchiveStoreLayoutRequest(
    int ExpectedRevision);
