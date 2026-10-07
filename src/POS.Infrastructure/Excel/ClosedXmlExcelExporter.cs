using ClosedXML.Excel;
using POS.Application.Abstractions.Excel;

namespace POS.Infrastructure.Excel;

internal sealed class ClosedXmlExcelExporter : IExcelExporter
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

        int currentRow = 1;

        // 1. Optional Title
        if (!string.IsNullOrWhiteSpace(options.Title))
        {
            worksheet.Cell(currentRow, 1).Value = options.Title;
            worksheet.Cell(currentRow, 1).Style.Font.Bold = true;
            worksheet.Cell(currentRow, 1).Style.Font.FontSize = 16;
            worksheet.Cell(currentRow, 1).Style.Font.FontColor = XLColor.FromHtml(options.HeaderBackgroundColor);
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

        // 4. Set explicit column widths or AutoFit
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
