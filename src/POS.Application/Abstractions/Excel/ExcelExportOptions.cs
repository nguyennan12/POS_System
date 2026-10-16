namespace POS.Application.Abstractions.Excel;

public class ExcelExportColumn<T>
{
    public string Header { get; set; } = string.Empty;
    public Func<T, object?> ValueSelector { get; set; } = default!;
    public string? NumberFormat { get; set; }
    public double? Width { get; set; }
    public bool IsTotalColumn { get; set; }
    public Func<IEnumerable<T>, object?>? CustomTotalSelector { get; set; }

    public ExcelExportColumn(
        string header,
        Func<T, object?> valueSelector,
        string? numberFormat = null,
        double? width = null,
        bool isTotalColumn = false,
        Func<IEnumerable<T>, object?>? customTotalSelector = null)
    {
        Header = header;
        ValueSelector = valueSelector;
        NumberFormat = numberFormat;
        Width = width;
        IsTotalColumn = isTotalColumn;
        CustomTotalSelector = customTotalSelector;
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
    public string FontName { get; set; } = "Segoe UI";
    public double FontSize { get; set; } = 11;
    public bool ShowTotalRow { get; set; } = false;
    public string TotalRowLabel { get; set; } = "Tổng cộng";
    public int TotalRowLabelColumnIndex { get; set; } = 0; // 0-based column index

    public ExcelExportOptions<T> AddColumn(
        string header,
        Func<T, object?> valueSelector,
        string? numberFormat = null,
        double? width = null,
        bool isTotalColumn = false,
        Func<IEnumerable<T>, object?>? customTotalSelector = null)
    {
        Columns.Add(new ExcelExportColumn<T>(header, valueSelector, numberFormat, width, isTotalColumn, customTotalSelector));
        return this;
    }
}
