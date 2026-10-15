using POS.Domain.Common;

namespace POS.Application.UseCases.Inventory.StockTake;

public static class StockTakeErrors
{
    public static readonly Error Unauthorized =
        new(ErrorType.Unauthorized, "StockTake.Unauthorized", "Không có quyền thực hiện thao tác này.");

    public static readonly Error ManagerRequired =
        new(ErrorType.Unauthorized, "StockTake.ManagerRequired", "Chỉ Quản lý mới có quyền phê duyệt kiểm kê kho.");

    public static readonly Error StoreRequired =
        new(ErrorType.Invalid, "StockTake.StoreRequired", "Nhân viên phải thuộc một cửa hàng.");

    public static readonly Error StoreInactive =
        new(ErrorType.Invalid, "StockTake.StoreInactive", "Cửa hàng không hoạt động.");

    public static readonly Error NotFound =
        new(ErrorType.NotFound, "StockTake.NotFound", "Không tìm thấy phiếu kiểm kê.");

    public static readonly Error AccessDenied =
        new(ErrorType.Unauthorized, "StockTake.AccessDenied", "Không có quyền thao tác phiếu kiểm kê này.");

    public static readonly Error AlreadyApproved =
        new(ErrorType.Invalid, "StockTake.AlreadyApproved", "Phiếu kiểm kê đã được duyệt trước đó.");

    public static readonly Error NoItems =
        new(ErrorType.Validation, "StockTake.NoItems", "Phiếu kiểm kê phải có ít nhất một mặt hàng.");

    public static Error SkuNotFound(Guid skuId) =>
        new(ErrorType.NotFound, "StockTake.SkuNotFound", $"Không tìm thấy SKU: {skuId}.");
}
