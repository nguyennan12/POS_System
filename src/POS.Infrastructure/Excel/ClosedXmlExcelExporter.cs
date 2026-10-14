using ClosedXML.Excel;
using POS.Application.Abstractions.Excel;

namespace POS.Infrastructure.Excel;

public sealed class ClosedXmlExcelExporter : IExcelExporter
{
    public byte[] Export<T>(IEnumerable<T> data, Action<ExcelExportOptions<T>> configure)
    {
        using var stream = new MemoryStream();
        Export(data, stream, configure);
        return stream.ToArray();
    }

    public void Export<T>(IEnumerable<T> data, Stream outputStream, Action<ExcelExportOptions<T>> configure)
    {
        var options = new ExcelExportOptions<T>();
        configure(options);

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add(options.SheetName);
        worksheet.Style.Font.FontName = options.FontName;
        worksheet.Style.Font.FontSize = options.FontSize;

        int currentRow = 1;

        // 1. Optional Title
        if (!string.IsNullOrWhiteSpace(options.Title))
        {
            var titleCell = worksheet.Cell(currentRow, 1);
            titleCell.Value = options.Title;
            titleCell.Style.Font.Bold = true;
            titleCell.Style.Font.FontSize = 16;
            titleCell.Style.Font.FontName = options.FontName;
            titleCell.Style.Font.FontColor = XLColor.FromHtml(options.HeaderBackgroundColor);
            currentRow += 2;
        }

        int headerRowIndex = currentRow;

        // 2. Render Headers
        for (int c = 0; c < options.Columns.Count; c++)
        {
            var col = options.Columns[c];
            var cell = worksheet.Cell(headerRowIndex, c + 1);
            cell.Value = col.Header;
            cell.Style.Font.Bold = true;
            cell.Style.Font.FontName = options.FontName;
            cell.Style.Font.FontColor = XLColor.FromHtml(options.HeaderTextColor);
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml(options.HeaderBackgroundColor);
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            cell.Style.Border.OutsideBorderColor = XLColor.Black;
        }

        worksheet.Row(headerRowIndex).Height = 24;

        // 3. Render Data Rows
        var dataList = data.ToList();
        for (int r = 0; r < dataList.Count; r++)
        {
            int dataRowIndex = headerRowIndex + 1 + r;
            var item = dataList[r];

            for (int c = 0; c < options.Columns.Count; c++)
            {
                var col = options.Columns[c];
                var cell = worksheet.Cell(dataRowIndex, c + 1);
                var rawValue = col.ValueSelector(item);

                if (rawValue != null)
                {
                    if (rawValue is decimal decVal)
                    {
                        cell.Value = decVal;
                        cell.Style.NumberFormat.Format = col.NumberFormat ?? "#,##0";
                        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                    }
                    else if (rawValue is int intVal)
                    {
                        cell.Value = intVal;
                        cell.Style.NumberFormat.Format = col.NumberFormat ?? "#,##0";
                        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                    }
                    else if (rawValue is DateTime dtVal)
                    {
                        cell.Value = dtVal;
                        cell.Style.NumberFormat.Format = col.NumberFormat ?? "dd/MM/yyyy HH:mm";
                        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    }
                    else if (rawValue is bool boolVal)
                    {
                        cell.Value = boolVal ? "Có" : "Không";
                        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    }
                    else
                    {
                        cell.Value = rawValue.ToString();
                    }
                }

                cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                cell.Style.Border.OutsideBorderColor = XLColor.LightGray;
            }

            // Alternating zebra row background
            if (r % 2 == 1)
            {
                worksheet.Range(dataRowIndex, 1, dataRowIndex, options.Columns.Count).Style.Fill.BackgroundColor = XLColor.FromHtml("#F9FAFB");
            }
        }

        // 4. Render Total Row if enabled
        if (options.ShowTotalRow && dataList.Count > 0)
        {
            int totalRowIndex = headerRowIndex + 1 + dataList.Count;
            var totalRange = worksheet.Range(totalRowIndex, 1, totalRowIndex, options.Columns.Count);
            totalRange.Style.Font.Bold = true;
            totalRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#F1F5F9");
            totalRange.Style.Border.TopBorder = XLBorderStyleValues.Thin;
            totalRange.Style.Border.BottomBorder = XLBorderStyleValues.Double;
            totalRange.Style.Border.TopBorderColor = XLColor.Black;
            totalRange.Style.Border.BottomBorderColor = XLColor.Black;

            // Set total row label
            int labelCol = Math.Clamp(options.TotalRowLabelColumnIndex + 1, 1, options.Columns.Count);
            worksheet.Cell(totalRowIndex, labelCol).Value = options.TotalRowLabel;

            // Calculate totals for specified columns
            for (int c = 0; c < options.Columns.Count; c++)
            {
                var col = options.Columns[c];
                var cell = worksheet.Cell(totalRowIndex, c + 1);
                cell.Style.Border.LeftBorder = XLBorderStyleValues.Thin;
                cell.Style.Border.RightBorder = XLBorderStyleValues.Thin;

                if (col.CustomTotalSelector != null)
                {
                    var customTotal = col.CustomTotalSelector(dataList);
                    if (customTotal is decimal customDec)
                    {
                        cell.Value = customDec;
                        cell.Style.NumberFormat.Format = col.NumberFormat ?? "#,##0";
                        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                    }
                    else if (customTotal is int customInt)
                    {
                        cell.Value = customInt;
                        cell.Style.NumberFormat.Format = col.NumberFormat ?? "#,##0";
                        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                    }
                    else if (customTotal != null)
                    {
                        cell.Value = customTotal.ToString();
                    }
                }
                else if (col.IsTotalColumn)
                {
                    decimal sumDec = 0;
                    bool hasDecimals = false;
                    int sumInt = 0;
                    bool hasInts = false;

                    foreach (var item in dataList)
                    {
                        var val = col.ValueSelector(item);
                        if (val is decimal d)
                        {
                            sumDec += d;
                            hasDecimals = true;
                        }
                        else if (val is int i)
                        {
                            sumInt += i;
                            hasInts = true;
                        }
                    }

                    if (hasDecimals)
                    {
                        cell.Value = sumDec;
                        cell.Style.NumberFormat.Format = col.NumberFormat ?? "#,##0";
                        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                    }
                    else if (hasInts)
                    {
                        cell.Value = sumInt;
                        cell.Style.NumberFormat.Format = col.NumberFormat ?? "#,##0";
                        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                    }
                }
            }

            worksheet.Row(totalRowIndex).Height = 22;
        }

        // 5. Set explicit column widths or AutoFit
        for (int c = 0; c < options.Columns.Count; c++)
        {
            var col = options.Columns[c];
            if (col.Width.HasValue)
            {
                worksheet.Column(c + 1).Width = col.Width.Value;
            }
            else if (options.AutoFitColumns)
            {
                worksheet.Column(c + 1).AdjustToContents();
            }
        }

        workbook.SaveAs(outputStream);
    }
}
