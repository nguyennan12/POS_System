using POS.Application.Abstractions.Excel;

namespace POS.Application.UseCases.Inventory.StockIn.Commands.ImportStockInVoucher;

/// <summary>
/// DTO ánh xạ từng dòng trong bảng kê giao hàng / phiếu nhập kho Excel của Nhà Cung Cấp.
/// Hỗ trợ tìm kiếm theo cả Mã SKU lẫn Mã Vạch (Barcode).
/// </summary>
public class StockInImportRowDto
{
    [ExcelColumn("SkuCode", "Mã SKU", "SKU", Order = 1)]
    public string? SkuCode { get; set; }

    [ExcelColumn("Barcode", "Mã vạch", Order = 2)]
    public string? Barcode { get; set; }

    [ExcelColumn("Qty", "Quantity", "Số lượng", "SL", "Số lượng nhập", Order = 3)]
    public decimal? Qty { get; set; }

    [ExcelColumn("UnitPrice", "UnitCost", "CostPrice", "Đơn giá", "Giá nhập", "Đơn giá nhập", Order = 4)]
    public decimal? UnitPrice { get; set; }

    [ExcelColumn("BatchNo", "Số lô", "Mã lô", "Lô", Order = 5)]
    public string? BatchNo { get; set; }

    [ExcelColumn("ExpiryDate", "Hạn sử dụng", "HSD", Order = 6)]
    public DateOnly? ExpiryDate { get; set; }
}
