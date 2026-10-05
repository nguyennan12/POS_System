namespace POS.Application.Abstractions.Import;

/// <summary>Kết quả parse một dòng dữ liệu từ file Excel.</summary>
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
    string? Brand
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
