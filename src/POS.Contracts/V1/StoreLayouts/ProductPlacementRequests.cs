namespace POS.Contracts.V1.StoreLayouts;

public sealed record AssignProductPlacementRequest(
    Guid LayoutId,
    Guid ShelfLevelId,
    Guid SkuId,
    bool IsPrimary,
    string? Note,
    int ExpectedRevision);

public sealed record MoveProductPlacementRequest(
    Guid LayoutId,
    Guid TargetShelfLevelId,
    string? Note,
    int ExpectedRevision);

public sealed record SetPrimaryPlacementRequest(
    Guid LayoutId,
    int ExpectedRevision);

public sealed record RemoveProductPlacementRequest(
    Guid LayoutId,
    string? Note,
    int ExpectedRevision);
