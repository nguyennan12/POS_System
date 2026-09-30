using POS.Domain.Common;

namespace POS.Application.UseCases.Invoices.Errors;

public static class InvoiceReadErrors
{
    public static readonly Error InvoiceNotFound = new(
        ErrorType.NotFound, "INVOICE.NOT_FOUND", "Không tìm thấy hóa đơn.");

    public static readonly Error Unauthorized = new(
        ErrorType.Unauthorized, "INVOICE.UNAUTHORIZED", "Yêu cầu đăng nhập để xem hóa đơn.");

    public static readonly Error InvalidStore = new(
        ErrorType.Forbidden, "INVOICE.INVALID_STORE", "Nhân viên không có quyền xem hóa đơn của cửa hàng này.");
}
