using POS.Domain.Common;

namespace POS.Application.UseCases.Inventory;

public static class InventoryErrors
{
    public static readonly Error Unauthorized =
        new(ErrorType.Unauthorized, "Inventory.Unauthorized", "Không có quyền thực hiện thao tác này.");

    public static readonly Error StoreRequired =
        new(ErrorType.Invalid, "Inventory.StoreRequired", "Nhân viên phải thuộc một cửa hàng.");

    public static readonly Error StoreInactive =
        new(ErrorType.Invalid, "Inventory.StoreInactive", "Cửa hàng không hoạt động.");

    public static Error SkuNotFound(Guid skuId) =>
        new(ErrorType.NotFound, "Inventory.SkuNotFound", $"Không tìm thấy tồn kho cho SKU: {skuId}.");

    public static Error InsufficientStock(Guid skuId, decimal available, decimal requested) =>
        new(ErrorType.Invalid, "Inventory.InsufficientStock",
            $"Tồn kho không đủ cho SKU {skuId}: hiện có {available}, yêu cầu {requested}.");

    public static Error BatchNotFound(Guid batchId) =>
        new(ErrorType.NotFound, "Inventory.BatchNotFound", $"Không tìm thấy lô hàng: {batchId}.");

    public static Error InsufficientBatchStock(Guid batchId, decimal available, decimal requested) =>
        new(ErrorType.Invalid, "Inventory.InsufficientBatchStock",
            $"Số lượng lô {batchId} không đủ: hiện có {available}, yêu cầu {requested}.");
}
