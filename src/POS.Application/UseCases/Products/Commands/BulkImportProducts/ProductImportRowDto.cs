using POS.Application.Abstractions.Excel;

namespace POS.Application.UseCases.Products.Commands.BulkImportProducts;

/// <summary>
/// DTO ánh xạ từng dòng Excel khi nhập danh mục sản phẩm hàng loạt.
/// Dùng với <see cref="IExcelReader"/> generic engine — header cột được khớp
/// theo <see cref="ExcelColumnAttribute.Name"/> (case-insensitive).
///
/// Layout template Excel (header row 1, data từ row 2):
/// | Name | CategoryName | BaseUnit | SkuCode | Barcode | CostPrice | SellPrice | TaxRate | Brand | ImageUrl | Attributes |
/// |  1   |      2       |    3     |    4    |    5    |     6     |     7     |    8    |   9   |    10    |     11     |
/// </summary>
public class ProductImportRowDto
{
    /// <summary>Tên sản phẩm.</summary>
    [ExcelColumn("Name", "Tên sản phẩm", "Tên SP", Order = 1)]
    public string? Name { get; set; }

    /// <summary>Tên danh mục — phải tồn tại trong Store.</summary>
    [ExcelColumn("CategoryName", "Danh mục", "Category", Order = 2)]
    public string? CategoryName { get; set; }

    /// <summary>Đơn vị cơ bản (ví dụ: Cái, Hộp, Kg).</summary>
    [ExcelColumn("BaseUnit", "Đơn vị", "Unit", Order = 3)]
    public string? BaseUnit { get; set; }

    /// <summary>Mã SKU — duy nhất trong cửa hàng.</summary>
    [ExcelColumn("SkuCode", "Mã SKU", "SKU", Order = 4)]
    public string? SkuCode { get; set; }

    /// <summary>Mã vạch barcode.</summary>
    [ExcelColumn("Barcode", "Mã vạch", "Barcode", Order = 5)]
    public string? Barcode { get; set; }

    /// <summary>Giá vốn nhập hàng.</summary>
    [ExcelColumn("CostPrice", "Giá vốn", Order = 6)]
    public decimal? CostPrice { get; set; }

    /// <summary>Giá bán lẻ.</summary>
    [ExcelColumn("SellPrice", "Giá bán", Order = 7)]
    public decimal? SellPrice { get; set; }

    /// <summary>
    /// Thuế suất VAT — hỗ trợ cả dạng % (10) lẫn dạng thập phân (0.1).
    /// Handler sẽ tự chuẩn hóa về dạng % trước khi validate.
    /// </summary>
    [ExcelColumn("TaxRate", "Thuế suất", "VAT", Order = 8)]
    public decimal? TaxRate { get; set; }

    /// <summary>Thương hiệu (tùy chọn).</summary>
    [ExcelColumn("Brand", "Thương hiệu", Order = 9)]
    public string? Brand { get; set; }

    /// <summary>URL ảnh sản phẩm (tùy chọn).</summary>
    [ExcelColumn("ImageUrl", "Ảnh", "Image", Order = 10)]
    public string? ImageUrl { get; set; }

    /// <summary>
    /// Chuỗi JSON thuộc tính biến thể SKU, e.g. {"Màu":"Đỏ","Size":"M"}.
    /// Tùy chọn — bỏ trống nếu sản phẩm không có biến thể.
    /// </summary>
    [ExcelColumn("Attributes", "Thuộc tính", "Variants", Order = 11)]
    public string? Attributes { get; set; }
}
