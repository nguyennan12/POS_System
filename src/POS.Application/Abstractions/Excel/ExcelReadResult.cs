namespace POS.Application.Abstractions.Excel;

public record ExcelRowResult<T>(int RowNumber, T Item, IReadOnlyList<string> Errors) where T : class;

public record ExcelReadResult<T>(
    IReadOnlyList<ExcelRowResult<T>> Rows,
    IReadOnlyList<string> GlobalErrors
) where T : class
{
    public bool HasErrors => GlobalErrors.Count > 0 || Rows.Any(r => r.Errors.Count > 0);
    public IReadOnlyList<T> SuccessfulItems => Rows.Where(r => r.Errors.Count == 0).Select(r => r.Item).ToList();
}
