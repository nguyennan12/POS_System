using POS.Domain.Common;

namespace POS.Application.UseCases.Invoices.Errors;

public static class InvoiceReadErrors
{
    public static readonly Error InvalidTimezone = new(
        ErrorType.Invalid, "INVOICE.INVALID_TIMEZONE", "Múi giờ cửa hàng không hợp lệ.");
    public static readonly Error ReceiptDataUnavailable = new(
        ErrorType.Invalid, "INVOICE.RECEIPT_DATA_UNAVAILABLE", "Thiếu thông tin cửa hàng hoặc thu ngân gốc.");
    public static readonly Error RenderingFailed = new(
        ErrorType.Invalid, "INVOICE.RENDERING_FAILED", "Không thể xuất hóa đơn.");
    public static readonly Error InvoiceNotFound = new(
        ErrorType.NotFound, "INVOICE.NOT_FOUND", "Không tìm thấy hóa đơn.");

    public static readonly Error Unauthorized = new(
        ErrorType.Unauthorized, "INVOICE.UNAUTHORIZED", "Yêu cầu đăng nhập để xem hóa đơn.");

    public static readonly Error InvalidStore = new(
        ErrorType.Forbidden, "INVOICE.INVALID_STORE", "Nhân viên không có quyền xem hóa đơn của cửa hàng này.");
}
