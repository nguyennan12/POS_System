namespace POS.Application.Abstractions.Excel;

public class ExcelExportColumn<T>
{
    public string Header { get; set; } = string.Empty;
    public Func<T, object?> ValueSelector { get; set; } = default!;
    public string? NumberFormat { get; set; }
    public double? Width { get; set; }

    public ExcelExportColumn(string header, Func<T, object?> valueSelector, string? numberFormat = null, double? width = null)
    {
        Header = header;
        ValueSelector = valueSelector;
        NumberFormat = numberFormat;
        Width = width;
    }
}

public class ExcelExportOptions<T>
{
    public string SheetName { get; set; } = "Sheet1";
    public string? Title { get; set; }
    public List<ExcelExportColumn<T>> Columns { get; set; } = new();
    public bool AutoFitColumns { get; set; } = true;
    public string HeaderBackgroundColor { get; set; } = "#1F4E79"; // Deep blue primary
    public string HeaderTextColor { get; set; } = "#FFFFFF";

    public ExcelExportOptions<T> AddColumn(string header, Func<T, object?> valueSelector, string? numberFormat = null, double? width = null)
    {
        Columns.Add(new ExcelExportColumn<T>(header, valueSelector, numberFormat, width));
        return this;
    }
}
