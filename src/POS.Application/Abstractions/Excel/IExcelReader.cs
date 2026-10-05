namespace POS.Application.Abstractions.Excel;

public interface IExcelReader
{
    /// <summary>
    /// Đọc dữ liệu từ file Excel (stream hoặc byte[]) và ánh xạ vào kiểu dữ liệu T dựa trên header cột hoặc thuộc tính ExcelColumn.
    /// </summary>
    ExcelReadResult<T> Read<T>(Stream stream, int headerRowIndex = 1, int dataStartRowIndex = 2) where T : class, new();

    /// <summary>
    /// Đọc dữ liệu từ mảng byte file Excel.
    /// </summary>
    ExcelReadResult<T> Read<T>(byte[] fileBytes, int headerRowIndex = 1, int dataStartRowIndex = 2) where T : class, new();
}
