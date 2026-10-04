using System.Globalization;
using ClosedXML.Excel;
using POS.Application.Abstractions.Import;

namespace POS.Infrastructure.Import;

/// <summary>
/// Parse file Excel (.xlsx/.xls) thành danh sách ExcelProductRow.
/// Layout cột (1-indexed):
///   1=Name  2=CategoryName  3=BaseUnit  4=SkuCode  5=Barcode
///   6=CostPrice  7=SellPrice  8=TaxRate  9=Brand  10=ImageUrl  11=AttributesJson
/// </summary>
internal sealed class ClosedXmlExcelImportParser : IExcelImportParser
{
    public ExcelParseResult Parse(byte[] fileContent)
    {
        var rows   = new List<ExcelProductRow>();
        var errors = new List<string>();

        try
        {
            using var stream    = new MemoryStream(fileContent);
            using var workbook  = new XLWorkbook(stream);
            var worksheet = workbook.Worksheets.FirstOrDefault();

            if (worksheet == null)
            {
                errors.Add("File Excel không có trang tính (worksheet) nào.");
                return new ExcelParseResult(rows, errors);
            }

            var lastRowUsed = worksheet.LastRowUsed();
            if (lastRowUsed == null || lastRowUsed.RowNumber() < 2)
            {
                errors.Add("File Excel không có dữ liệu hoặc thiếu dòng tiêu đề.");
                return new ExcelParseResult(rows, errors);
            }

            for (int r = 2; r <= lastRowUsed.RowNumber(); r++)
            {
                var row = worksheet.Row(r);
                if (row.IsEmpty()) continue;

                var name     = GetString(row.Cell(1));
                var catName  = GetString(row.Cell(2));
                var baseUnit = GetString(row.Cell(3));
                var skuCode  = GetString(row.Cell(4));
                var barcode  = GetString(row.Cell(5));

                // Bỏ qua dòng hoàn toàn trống (ô Name và SkuCode đều rỗng)
                if (string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(skuCode))
                    continue;

                var costPriceStr = GetString(row.Cell(6));
                var sellPriceStr = GetString(row.Cell(7));
                var taxRateStr   = GetString(row.Cell(8));
                var brand        = GetString(row.Cell(9));
                var imageUrl     = GetString(row.Cell(10));
                var attributesJson = GetString(row.Cell(11));

                decimal? costPrice = ParseDecimal(costPriceStr, r, "Giá vốn (CostPrice)", errors);
                decimal? sellPrice = ParseDecimal(sellPriceStr, r, "Giá bán (SellPrice)", errors);
                decimal? taxRate   = ParseDecimal(taxRateStr,   r, "Thuế suất (TaxRate)",  errors);

                rows.Add(new ExcelProductRow(
                    RowNumber:      r,
                    Name:           name,
                    CategoryName:   catName,
                    BaseUnit:       baseUnit,
                    SkuCode:        skuCode,
                    Barcode:        barcode,
                    CostPrice:      costPrice,
                    SellPrice:      sellPrice,
                    TaxRate:        taxRate,
                    Brand:          brand,
                    ImageUrl:       imageUrl,
                    AttributesJson: attributesJson
                ));
            }
        }
        catch (Exception ex)
        {
            errors.Add($"Lỗi khi xử lý file Excel: {ex.Message}");
        }

        return new ExcelParseResult(rows, errors);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static string? GetString(IXLCell cell)
        => cell.Value.ToString()?.Trim();

    /// <summary>
    /// Cố gắng parse decimal, ghi lỗi vào <paramref name="errors"/> nếu thất bại.
    /// Trả về null nếu ô rỗng.
    /// </summary>
    private static decimal? ParseDecimal(string? raw, int rowNumber, string columnLabel, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;

        if (decimal.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var val))
            return val;
        if (decimal.TryParse(raw, NumberStyles.Any, new CultureInfo("vi-VN"), out val))
            return val;

        errors.Add($"Dòng {rowNumber}: Cột {columnLabel} không đúng định dạng số (giá trị: '{raw}').");
        return null;
    }
}
