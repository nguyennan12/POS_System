using POS.Domain.Common;

namespace POS.Application.UseCases.Payments.Errors;

public static class PaymentErrors
{
    public static readonly Error PaymentNotFound = new(
        ErrorType.NotFound, "PAYMENT.NOT_FOUND", "Không tìm thấy thanh toán.");

    public static readonly Error Unauthorized = new(
        ErrorType.Unauthorized, "PAYMENT.UNAUTHORIZED", "Yêu cầu đăng nhập để xem thanh toán.");

    public static readonly Error InvalidStore = new(
        ErrorType.Forbidden, "PAYMENT.INVALID_STORE", "Nhân viên không có quyền xem thanh toán của cửa hàng này.");
}
