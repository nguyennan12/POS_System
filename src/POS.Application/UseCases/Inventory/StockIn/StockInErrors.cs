using POS.Domain.Common;

namespace POS.Application.UseCases.Inventory.StockIn;

public static class StockInErrors
{
    public static readonly Error Unauthorized =
        new(ErrorType.Unauthorized, "StockIn.Unauthorized", "Không có quyền thực hiện thao tác này.");

    public static readonly Error StoreRequired =
        new(ErrorType.Invalid, "StockIn.StoreRequired", "Nhân viên phải thuộc một cửa hàng.");

    public static readonly Error StoreInactive =
        new(ErrorType.Invalid, "StockIn.StoreInactive", "Cửa hàng không hoạt động.");

    public static readonly Error SupplierNotFound =
        new(ErrorType.NotFound, "StockIn.SupplierNotFound", "Không tìm thấy nhà cung cấp.");

    public static readonly Error NoItems =
        new(ErrorType.Validation, "StockIn.NoItems", "Phiếu nhập phải có ít nhất một mặt hàng.");

    public static readonly Error VoucherNotFound =
        new(ErrorType.NotFound, "StockIn.VoucherNotFound", "Không tìm thấy phiếu nhập kho.");

    public static readonly Error AccessDenied =
        new(ErrorType.Unauthorized, "StockIn.AccessDenied", "Không có quyền thao tác phiếu nhập này.");

    public static Error SkuNotFound(Guid skuId) =>
        new(ErrorType.NotFound, "StockIn.SkuNotFound", $"Không tìm thấy SKU: {skuId}.");
}
