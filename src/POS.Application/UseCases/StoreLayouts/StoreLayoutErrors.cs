using POS.Domain.Common;
using DomainErrors = POS.Domain.StoreLayouts.Errors.StoreLayoutErrors;

namespace POS.Application.UseCases.StoreLayouts;

public static class StoreLayoutErrors
{
    public static Error NotFound => DomainErrors.NotFound;
    public static Error NotDraft => DomainErrors.NotDraft;
    public static Error RevisionConflict => DomainErrors.RevisionConflict;
    public static Error ActiveExists => DomainErrors.ActiveExists;
    public static Error InvalidGrid => DomainErrors.InvalidGrid;
    public static Error EntranceRequired => DomainErrors.EntranceRequired;
    public static Error ShelfOverlap => DomainErrors.ShelfOverlap;
    public static Error ShelfOutOfBounds => DomainErrors.ShelfOutOfBounds;
    public static Error ShelfCellBlocked => DomainErrors.ShelfCellBlocked;
    public static Error UnreachableShelf => DomainErrors.UnreachableShelf;
    public static Error StoreMismatch => DomainErrors.StoreMismatch;
    public static Error ProductPlacementNotFound => DomainErrors.ProductPlacementNotFound;
    public static Error PrimaryConflict => DomainErrors.PrimaryConflict;
    public static Error SkuNotInStore => DomainErrors.SkuNotInStore;
    public static Error ZoneNotFound => DomainErrors.ZoneNotFound;
    public static Error ShelfNotFound => DomainErrors.ShelfNotFound;
    public static Error ShelfLevelNotFound => DomainErrors.ShelfLevelNotFound;
    public static Error ShelfHasPlacements => DomainErrors.ShelfHasPlacements;

    public static readonly Error StoreRequired = new(
        ErrorType.Validation,
        "STORE_REQUIRED",
        "Vui lòng chỉ định cửa hàng để thực hiện thao tác.");
}
