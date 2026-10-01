using POS.Domain.Common;

namespace POS.Application.UseCases.Products;

public static class ProductErrors
{
    public static Error ProductNotFound(Guid id) =>
        new(ErrorType.NotFound, "Product.NotFound", $"Sản phẩm với ID '{id}' không tồn tại.");

    public static Error SkuNotFound(Guid id) =>
        new(ErrorType.NotFound, "Sku.NotFound", $"SKU với ID '{id}' không tồn tại.");

    public static readonly Error BarcodeNotFound =
        new(ErrorType.NotFound, "Sku.BarcodeNotFound", "Không tìm thấy sản phẩm với mã vạch này.");

    public static readonly Error StoreRequired =
        new(ErrorType.Unauthorized, "Store.Required", "Thiếu ngữ cảnh cửa hàng.");

    public static readonly Error InvalidPrice =
        new(ErrorType.Validation, "Product.InvalidPrice", "Giá bán và giá vốn phải lớn hơn hoặc bằng 0.");

    public static readonly Error InvalidTaxRate =
        new(ErrorType.Validation, "Product.InvalidTaxRate", "Thuế suất VAT phải là 0, 5, 8, hoặc 10.");

    public static readonly Error SkuCodeExists =
        new(ErrorType.Validation, "Sku.SkuCodeExists", "Mã SKU này đã tồn tại trong cửa hàng.");

    public static readonly Error BarcodeExists =
        new(ErrorType.Validation, "Sku.BarcodeExists", "Mã vạch này đã tồn tại trong cửa hàng.");

    public static Error CategoryNotFound(Guid id) =>
        new(ErrorType.NotFound, "Category.NotFound", $"Danh mục với ID '{id}' không tồn tại hoặc không thuộc cửa hàng này.");

    public static readonly Error InvalidStatus =
        new(ErrorType.Validation, "Product.InvalidStatus", "Trạng thái sản phẩm không hợp lệ. Giá trị hợp lệ: Active, Inactive, Discontinued.");

    public static readonly Error EmployeeRequired =
        new(ErrorType.Unauthorized, "Employee.Required", "Cần có danh tính nhân viên để thực hiện thao tác này.");

    public static Error SkuInactive(string skuCode) =>
        new(ErrorType.Validation, "Sku.Inactive", $"SKU '{skuCode}' đang ngừng bán.");

    public static Error ProductInactive(string productName) =>
        new(ErrorType.Validation, "Product.Inactive", $"Sản phẩm '{productName}' đang không hoạt động.");

    public static Error UnitConversionDuplicate(string unitName) =>
        new(ErrorType.Validation, "UnitConversion.Duplicate", $"Đơn vị quy đổi '{unitName}' đã tồn tại cho SKU này.");
}
