namespace POS.Application.Abstractions.Excel;

public interface IExcelExporter
{
    /// <summary>
    /// Xuất danh sách dữ liệu ra mảng byte file Excel (.xlsx) với cấu hình cột và định dạng bảng biểu.
    /// </summary>
    byte[] Export<T>(IEnumerable<T> data, Action<ExcelExportOptions<T>> configure);

    /// <summary>
    /// Xuất danh sách dữ liệu trực tiếp vào một Stream.
    /// </summary>
    void Export<T>(IEnumerable<T> data, Stream outputStream, Action<ExcelExportOptions<T>> configure);
}
