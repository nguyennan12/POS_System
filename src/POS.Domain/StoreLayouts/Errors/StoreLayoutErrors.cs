using POS.Domain.Common;

namespace POS.Domain.StoreLayouts.Errors;

public static class StoreLayoutErrors
{
    public static readonly Error NotFound = new(
        ErrorType.NotFound,
        "STORE_LAYOUT_NOT_FOUND",
        "Sơ đồ mặt bằng không tồn tại.");

    public static readonly Error NotDraft = new(
        ErrorType.Invalid,
        "STORE_LAYOUT_NOT_DRAFT",
        "Chỉ có sơ đồ ở trạng thái Bản thảo (Draft) mới được phép chỉnh sửa.");

    public static readonly Error RevisionConflict = new(
        ErrorType.Invalid,
        "STORE_LAYOUT_REVISION_CONFLICT",
        "Sơ đồ đã bị thay đổi bởi người dùng khác. Vui lòng tải lại trang và thử lại.");

    public static readonly Error ActiveExists = new(
        ErrorType.AlreadyExists,
        "STORE_LAYOUT_ACTIVE_EXISTS",
        "Cửa hàng đã có sơ đồ đang hoạt động (Active).");

    public static readonly Error InvalidGrid = new(
        ErrorType.Validation,
        "STORE_LAYOUT_INVALID_GRID",
        "Kích thước lưới không hợp lệ (chiều rộng và chiều cao phải từ 4 đến 100 ô).");

    public static readonly Error EntranceRequired = new(
        ErrorType.Validation,
        "STORE_LAYOUT_ENTRANCE_REQUIRED",
        "Sơ đồ phải có ít nhất một lối vào (Entrance) trước khi xuất bản.");

    public static readonly Error ShelfOverlap = new(
        ErrorType.Invalid,
        "STORE_LAYOUT_SHELF_OVERLAP",
        "Vị trí kệ bị trùng hoặc đè lên kệ khác.");

    public static readonly Error ShelfOutOfBounds = new(
        ErrorType.Validation,
        "STORE_LAYOUT_SHELF_OUT_OF_BOUNDS",
        "Kệ nằm ngoài phạm vi lưới mặt bằng.");

    public static readonly Error ShelfCellBlocked = new(
        ErrorType.Validation,
        "STORE_LAYOUT_SHELF_BLOCKED_CELL",
        "Kệ không được đặt đè lên lối vào, quầy thu ngân hoặc ô bị chặn.");

    public static readonly Error UnreachableShelf = new(
        ErrorType.Validation,
        "STORE_LAYOUT_UNREACHABLE_SHELF",
        "Không thể tìm thấy lối đi từ cửa vào đến một hoặc nhiều kệ hàng.");

    public static readonly Error StoreMismatch = new(
        ErrorType.Validation,
        "STORE_LAYOUT_STORE_MISMATCH",
        "Dữ liệu liên quan không thuộc cùng một cửa hàng.");

    public static readonly Error ProductPlacementNotFound = new(
        ErrorType.NotFound,
        "PRODUCT_PLACEMENT_NOT_FOUND",
        "Vị trí gán sản phẩm không tồn tại.");

    public static readonly Error PrimaryConflict = new(
        ErrorType.AlreadyExists,
        "PRODUCT_PLACEMENT_PRIMARY_CONFLICT",
        "SKU đã có một vị trí chính (Primary) khác trên sơ đồ này.");

    public static readonly Error SkuNotInStore = new(
        ErrorType.Validation,
        "SKU_NOT_IN_STORE",
        "Sản phẩm / SKU không thuộc cửa hàng này hoặc đang không hoạt động.");

    public static readonly Error ZoneNotFound = new(
        ErrorType.NotFound,
        "STORE_ZONE_NOT_FOUND",
        "Khu vực không tồn tại trên sơ đồ.");

    public static readonly Error ShelfNotFound = new(
        ErrorType.NotFound,
        "SHELF_NOT_FOUND",
        "Kệ hàng không tồn tại trên sơ đồ.");

    public static readonly Error ShelfLevelNotFound = new(
        ErrorType.NotFound,
        "SHELF_LEVEL_NOT_FOUND",
        "Tầng kệ không tồn tại.");

    public static readonly Error ShelfHasPlacements = new(
        ErrorType.Invalid,
        "SHELF_HAS_PLACEMENTS",
        "Kệ hàng hoặc tầng kệ đang chứa sản phẩm, không thể xóa trực tiếp.");
}
