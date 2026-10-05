using System.Globalization;
using ClosedXML.Excel;
using POS.Application.Abstractions.Import;

namespace POS.Infrastructure.Import;

internal sealed class ClosedXmlExcelImportParser : IExcelImportParser
{
    public ExcelParseResult Parse(byte[] fileContent)
    {
        var rows = new List<ExcelProductRow>();
        var errors = new List<string>();

        try
        {
            using var stream = new MemoryStream(fileContent);
            using var workbook = new XLWorkbook(stream);
            var worksheet = workbook.Worksheets.FirstOrDefault();

            if (worksheet == null)
            {
                errors.Add("File Excel không có trang tính (worksheet) nào.");
                return new ExcelParseResult(rows, errors);
            }

            // Assume header is on row 1, data starts at row 2
            var lastRowUsed = worksheet.LastRowUsed();
            if (lastRowUsed == null || lastRowUsed.RowNumber() < 2)
            {
                errors.Add("File Excel không có dữ liệu hoặc thiếu dòng tiêu đề.");
                return new ExcelParseResult(rows, errors);
            }

            // Iterate rows
            for (int r = 2; r <= lastRowUsed.RowNumber(); r++)
            {
                var row = worksheet.Row(r);
                
                // Check if row is completely empty
                if (row.IsEmpty()) continue;

                var name = GetString(row.Cell(1));
                var categoryName = GetString(row.Cell(2));
                var baseUnit = GetString(row.Cell(3));
                var skuCode = GetString(row.Cell(4));
                var barcode = GetString(row.Cell(5));
                var costPriceStr = GetString(row.Cell(6));
                var sellPriceStr = GetString(row.Cell(7));
                var taxRateStr = GetString(row.Cell(8));
                var brand = GetString(row.Cell(9));
                // cell 10 could be attributes JSON, skipped for MVP based on contract

                // If all essential fields are empty, might be trailing empty formatting, skip
                if (string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(skuCode))
                    continue;

                decimal? costPrice = null;
                if (!string.IsNullOrWhiteSpace(costPriceStr))
                {
                    if (decimal.TryParse(costPriceStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var cp))
                        costPrice = cp;
                    else
                        errors.Add($"Dòng {r}: Giá vốn không đúng định dạng số.");
                }

                decimal? sellPrice = null;
                if (!string.IsNullOrWhiteSpace(sellPriceStr))
                {
                    if (decimal.TryParse(sellPriceStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var sp))
                        sellPrice = sp;
                    else
                        errors.Add($"Dòng {r}: Giá bán không đúng định dạng số.");
                }

                decimal? taxRate = null;
                if (!string.IsNullOrWhiteSpace(taxRateStr))
                {
                    if (decimal.TryParse(taxRateStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var tr))
                        taxRate = tr;
                    else
                        errors.Add($"Dòng {r}: Thuế suất không đúng định dạng số.");
                }

                rows.Add(new ExcelProductRow(
                    RowNumber: r,
                    Name: name,
                    CategoryName: categoryName,
                    BaseUnit: baseUnit,
                    SkuCode: skuCode,
                    Barcode: barcode,
                    CostPrice: costPrice,
                    SellPrice: sellPrice,
                    TaxRate: taxRate,
                    Brand: brand
                ));
            }
        }
        catch (Exception ex)
        {
            errors.Add($"Lỗi khi xử lý file Excel: {ex.Message}");
        }

        return new ExcelParseResult(rows, errors);
    }

    private static string? GetString(IXLCell cell)
    {
        return cell.Value.ToString()?.Trim();
    }
}
