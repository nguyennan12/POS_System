using POS.Domain.Common;

namespace POS.Application.UseCases.Payments.Errors;

public static class PaymentErrors
{
    public static readonly Error InvalidShift = new(ErrorType.Forbidden, "PAYMENT.INVALID_SHIFT", "Nhân viên không có quyền theo dõi thanh toán của ca này.");
    public static readonly Error InvalidWebhook = new(ErrorType.Invalid, "PAYMENT.INVALID_WEBHOOK", "Không thể xác nhận thanh toán.");
    public static readonly Error GatewayUnavailable = new(ErrorType.Invalid, "PAYMENT.GATEWAY_UNAVAILABLE", "Chưa thể tạo QR. Vui lòng thử lại.");
    public static readonly Error InvalidOrder = new(ErrorType.Invalid, "PAYMENT.INVALID_ORDER", "Đơn hàng chưa sẵn sàng thanh toán QR.");
    public static readonly Error InvalidAmount = new(ErrorType.Invalid, "PAYMENT.INVALID_AMOUNT", "Số tiền QR phải là số nguyên VND hợp lệ và lớn hơn 0.");
    public static readonly Error UnsupportedCurrency = new(ErrorType.Invalid, "PAYMENT.UNSUPPORTED_CURRENCY", "Thanh toán QR chỉ hỗ trợ VND.");
    public static readonly Error ShiftClosed = new(ErrorType.Invalid, "PAYMENT.SHIFT_CLOSED", "Ca làm việc không còn hợp lệ.");
    public static readonly Error InvalidMethod = new(ErrorType.Validation, "PAYMENT.INVALID_METHOD", "Phương thức QR không hợp lệ.");
    public static readonly Error PaymentNotFound = new(
        ErrorType.NotFound, "PAYMENT.NOT_FOUND", "Không tìm thấy thanh toán.");

    public static readonly Error Unauthorized = new(
        ErrorType.Unauthorized, "PAYMENT.UNAUTHORIZED", "Yêu cầu đăng nhập để xem thanh toán.");

    public static readonly Error InvalidStore = new(
        ErrorType.Forbidden, "PAYMENT.INVALID_STORE", "Nhân viên không có quyền xem thanh toán của cửa hàng này.");
}
