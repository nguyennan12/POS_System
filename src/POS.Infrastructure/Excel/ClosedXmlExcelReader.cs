using System.Globalization;
using System.Reflection;
using ClosedXML.Excel;
using POS.Application.Abstractions.Excel;

namespace POS.Infrastructure.Excel;

internal sealed class ClosedXmlExcelReader : IExcelReader
{
    public ExcelReadResult<T> Read<T>(byte[] fileBytes, int headerRowIndex = 1, int dataStartRowIndex = 2) where T : class, new()
    {
        using var stream = new MemoryStream(fileBytes);
        return Read<T>(stream, headerRowIndex, dataStartRowIndex);
    }

    public ExcelReadResult<T> Read<T>(Stream stream, int headerRowIndex = 1, int dataStartRowIndex = 2) where T : class, new()
    {
        var rowResults = new List<ExcelRowResult<T>>();
        var globalErrors = new List<string>();

        try
        {
            using var workbook = new XLWorkbook(stream);
            var worksheet = workbook.Worksheets.FirstOrDefault();

            if (worksheet == null)
            {
                globalErrors.Add("File Excel không có trang tính (worksheet) nào.");
                return new ExcelReadResult<T>(rowResults, globalErrors);
            }

            var lastRowUsed = worksheet.LastRowUsed();
            if (lastRowUsed == null || lastRowUsed.RowNumber() < dataStartRowIndex)
            {
                globalErrors.Add("File Excel không có dữ liệu hoặc thiếu dòng tiêu đề.");
                return new ExcelReadResult<T>(rowResults, globalErrors);
            }

            // 1. Map columns from header row
            var headerRow = worksheet.Row(headerRowIndex);
            var columnMap = BuildColumnMapping<T>(headerRow);

            if (columnMap.Count == 0)
            {
                globalErrors.Add("Không tìm thấy các cột tiêu đề phù hợp trong file Excel.");
                return new ExcelReadResult<T>(rowResults, globalErrors);
            }

            // 2. Read data rows
            for (int r = dataStartRowIndex; r <= lastRowUsed.RowNumber(); r++)
            {
                var row = worksheet.Row(r);
                if (row.IsEmpty()) continue;

                var item = new T();
                var rowErrors = new List<string>();
                bool hasAnyData = false;

                foreach (var (colIndex, prop) in columnMap)
                {
                    var cell = row.Cell(colIndex);
                    if (cell.IsEmpty()) continue;

                    hasAnyData = true;
                    try
                    {
                        var value = ExtractCellValue(cell, prop.PropertyType);
                        if (value != null)
                        {
                            prop.SetValue(item, value);
                        }
                    }
                    catch (Exception ex)
                    {
                        rowErrors.Add($"Cột '{GetPropertyDisplayName(prop)}': {ex.Message}");
                    }
                }

                if (hasAnyData)
                {
                    rowResults.Add(new ExcelRowResult<T>(r, item, rowErrors));
                }
            }
        }
        catch (Exception ex)
        {
            globalErrors.Add($"Lỗi khi xử lý file Excel: {ex.Message}");
        }

        return new ExcelReadResult<T>(rowResults, globalErrors);
    }

    private static Dictionary<int, PropertyInfo> BuildColumnMapping<T>(IXLRow headerRow)
    {
        var mapping = new Dictionary<int, PropertyInfo>();
        var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanWrite)
            .ToList();

        var lastCol = headerRow.LastCellUsed()?.Address.ColumnNumber ?? 0;

        for (int col = 1; col <= lastCol; col++)
        {
            var headerText = headerRow.Cell(col).Value.ToString()?.Trim();
            if (string.IsNullOrWhiteSpace(headerText)) continue;

            foreach (var prop in properties)
            {
                if (mapping.Values.Contains(prop)) continue;

                var attr = prop.GetCustomAttribute<ExcelColumnAttribute>();
                if (attr != null)
                {
                    if (string.Equals(attr.Name, headerText, StringComparison.OrdinalIgnoreCase) ||
                        attr.Aliases.Any(a => string.Equals(a, headerText, StringComparison.OrdinalIgnoreCase)))
                    {
                        mapping[col] = prop;
                        break;
                    }
                }
                else if (string.Equals(prop.Name, headerText, StringComparison.OrdinalIgnoreCase))
                {
                    mapping[col] = prop;
                    break;
                }
            }
        }

        // Fallback: If no headers matched by name, map by property order/ExcelColumn.Order or declaration order
        if (mapping.Count == 0)
        {
            int colIdx = 1;
            foreach (var prop in properties.OrderBy(p => p.GetCustomAttribute<ExcelColumnAttribute>()?.Order ?? 999))
            {
                if (colIdx <= lastCol)
                {
                    mapping[colIdx] = prop;
                    colIdx++;
                }
            }
        }

        return mapping;
    }

    private static object? ExtractCellValue(IXLCell cell, Type targetType)
    {
        var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;

        if (cell.IsEmpty()) return null;

        if (underlyingType == typeof(string))
        {
            return cell.Value.ToString()?.Trim();
        }

        if (underlyingType == typeof(decimal))
        {
            if (cell.DataType == XLDataType.Number)
                return Convert.ToDecimal(cell.GetDouble());

            var str = cell.Value.ToString()?.Trim();
            if (decimal.TryParse(str, NumberStyles.Any, CultureInfo.InvariantCulture, out var val))
                return val;
            if (decimal.TryParse(str, NumberStyles.Any, new CultureInfo("vi-VN"), out val))
                return val;

            throw new FormatException($"Không thể chuyển '{str}' thành số.");
        }

        if (underlyingType == typeof(int))
        {
            if (cell.DataType == XLDataType.Number)
                return Convert.ToInt32(cell.GetDouble());

            var str = cell.Value.ToString()?.Trim();
            if (int.TryParse(str, NumberStyles.Any, CultureInfo.InvariantCulture, out var val))
                return val;

            throw new FormatException($"Không thể chuyển '{str}' thành số nguyên.");
        }

        if (underlyingType == typeof(DateTime))
        {
            if (cell.DataType == XLDataType.DateTime)
                return cell.GetDateTime();

            var str = cell.Value.ToString()?.Trim();
            if (DateTime.TryParse(str, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
                return dt;
            if (DateTime.TryParse(str, new CultureInfo("vi-VN"), DateTimeStyles.None, out dt))
                return dt;

            throw new FormatException($"Không thể chuyển '{str}' thành ngày tháng.");
        }

        if (underlyingType == typeof(bool))
        {
            if (cell.DataType == XLDataType.Boolean)
                return cell.GetBoolean();

            var str = cell.Value.ToString()?.Trim();
            if (bool.TryParse(str, out var b))
                return b;

            return str is "1" or "Y" or "yes" or "true" or "x" or "X";
        }

        return Convert.ChangeType(cell.Value.ToString(), underlyingType);
    }

    private static string GetPropertyDisplayName(PropertyInfo prop)
    {
        var attr = prop.GetCustomAttribute<ExcelColumnAttribute>();
        return attr?.Name ?? prop.Name;
    }
}
