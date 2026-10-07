namespace POS.Application.Abstractions.Import;

/// <summary>
/// Kết quả parse một dòng dữ liệu sản phẩm/SKU từ file Excel.
/// Một dòng = một SKU; nhiều dòng cùng (CategoryName, Name, Brand, BaseUnit) = một Product nhiều SKU.
/// </summary>
public record ExcelProductRow(
    int RowNumber,
    string? Name,
    string? CategoryName,
    string? BaseUnit,
    string? SkuCode,
    string? Barcode,
    decimal? CostPrice,
    decimal? SellPrice,
    decimal? TaxRate,
    string? Brand,
    /// <summary>URL ảnh sản phẩm (cột ImageUrl / Ảnh).</summary>
    string? ImageUrl,
    /// <summary>Chuỗi JSON mô tả thuộc tính biến thể SKU, e.g. {"Màu":"Đỏ","Size":"M"}.</summary>
    string? AttributesJson
);

/// <summary>Kết quả parse toàn bộ file Excel.</summary>
public record ExcelParseResult(
    IReadOnlyList<ExcelProductRow> Rows,
    IReadOnlyList<string> ParseErrors
);

public interface IExcelImportParser
{
    /// <summary>
    /// Đọc file Excel (byte[]) và trả về danh sách các dòng dữ liệu raw.
    /// Mỗi dòng bị lỗi cấu trúc (ô thiếu, kiểu sai) sẽ được ghi vào ParseErrors.
    /// </summary>
    ExcelParseResult Parse(byte[] fileContent);
}
