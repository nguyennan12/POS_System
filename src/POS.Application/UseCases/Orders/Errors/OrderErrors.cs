using POS.Domain.Common;

namespace POS.Application.UseCases.Orders.Errors;

public static class OrderErrors
{
    public static readonly Error DuplicatePayment = new(
        ErrorType.AlreadyExists, "ORDER.DUPLICATE_PAYMENT",
        "Mã giao dịch của phương thức thanh toán đã được sử dụng.");

    public static readonly Error InvalidTransactionRef = new(
        ErrorType.Invalid, "ORDER.INVALID_TRANSACTION_REF",
        "Mã giao dịch phải có nội dung và không vượt quá 100 ký tự.");

    public static readonly Error InvalidPaymentState = new(
        ErrorType.Invalid, "ORDER.INVALID_PAYMENT_STATE",
        "Dữ liệu thanh toán hiện tại không hợp lệ.");

    public static readonly Error Unauthorized = new(
        ErrorType.Unauthorized,
        "ORDER.UNAUTHORIZED",
        "Yêu cầu đăng nhập để thực hiện thao tác trên đơn hàng.");

    public static readonly Error OrderNotFound = new(
        ErrorType.NotFound,
        "ORDER.NOT_FOUND",
        "Không tìm thấy đơn hàng.");

    public static readonly Error ShiftNotFound = new(
        ErrorType.NotFound,
        "ORDER.SHIFT_NOT_FOUND",
        "Không tìm thấy ca làm việc.");

    public static readonly Error ShiftClosed = new(
        ErrorType.Invalid,
        "ORDER.SHIFT_CLOSED",
        "Ca làm việc đã đóng. Không thể tạo hoặc chỉnh sửa đơn hàng.");

    public static readonly Error InvalidStore = new(
        ErrorType.Forbidden,
        "ORDER.INVALID_STORE",
        "Nhân viên không có quyền thao tác trên cửa hàng của đơn này.");

    public static readonly Error NotDraft = new(
        ErrorType.Invalid,
        "ORDER.NOT_DRAFT",
        "Chỉ có thể chỉnh sửa giỏ hàng khi đơn ở trạng thái Draft.");

    public static readonly Error SkuNotFound = new(
        ErrorType.NotFound,
        "ORDER.SKU_NOT_FOUND",
        "Không tìm thấy sản phẩm (SKU).");

    public static readonly Error SkuInactive = new(
        ErrorType.Invalid,
        "ORDER.SKU_INACTIVE",
        "Sản phẩm đang ngừng kinh doanh hoặc không khả dụng.");

    public static readonly Error InvalidQuantity = new(
        ErrorType.Invalid,
        "ORDER.INVALID_QUANTITY",
        "Số lượng sản phẩm phải lớn hơn 0.");

    public static readonly Error CustomerNotFound = new(
        ErrorType.NotFound,
        "ORDER.CUSTOMER_NOT_FOUND",
        "Không tìm thấy khách hàng.");

    public static readonly Error VoucherNotFound = new(
        ErrorType.NotFound,
        "ORDER.VOUCHER_NOT_FOUND",
        "Mã giảm giá không tồn tại hoặc đã hết hạn.");

    public static readonly Error VoucherExpired = new(
        ErrorType.Invalid,
        "ORDER.VOUCHER_EXPIRED",
        "Mã giảm giá đã hết hạn sử dụng.");

    public static readonly Error VoucherUsageLimitReached = new(
        ErrorType.Invalid,
        "ORDER.VOUCHER_LIMIT_REACHED",
        "Mã giảm giá đã hết lượt sử dụng.");

    public static readonly Error VoucherCustomerLimitReached = new(
        ErrorType.Invalid,
        "ORDER.VOUCHER_CUSTOMER_LIMIT_REACHED",
        "Khách hàng đã dùng hết lượt cho mã giảm giá này.");

    public static readonly Error VoucherInactive = new(
        ErrorType.Invalid,
        "ORDER.VOUCHER_INACTIVE",
        "Mã giảm giá đang bị tạm khóa.");

    public static readonly Error CartEmpty = new(
        ErrorType.Invalid,
        "ORDER.CART_EMPTY",
        "Giỏ hàng đang trống. Vui lòng thêm sản phẩm vào giỏ trước khi thực hiện thao tác.");

    public static readonly Error AlreadyPaid = new(
        ErrorType.Invalid,
        "ORDER.ALREADY_PAID",
        "Đơn hàng đã được thanh toán.");

    public static readonly Error AlreadyCancelled = new(
        ErrorType.Invalid,
        "ORDER.ALREADY_CANCELLED",
        "Đơn hàng đã bị hủy.");

    public static readonly Error CannotCancelPaidOrder = new(
        ErrorType.Invalid,
        "ORDER.CANNOT_CANCEL_PAID",
        "Không thể hủy đơn hàng đã thanh toán.");

    public static readonly Error CannotCancelOrderWithPayments = new(
        ErrorType.Invalid,
        "ORDER.CANNOT_CANCEL_WITH_PAYMENTS",
        "Không thể hủy đơn hàng đã phát sinh thanh toán thành công.");

    public static readonly Error ManagerOnly = new(
        ErrorType.Forbidden,
        "ORDER.MANAGER_ONLY",
        "Hủy đơn hàng yêu cầu quyền StoreManager hoặc Owner.");

    public static readonly Error InvalidPaymentMethod = new(
        ErrorType.Invalid,
        "ORDER.INVALID_PAYMENT_METHOD",
        "Phương thức thanh toán không hợp lệ.");

    public static readonly Error InvalidPaymentAmount = new(
        ErrorType.Invalid,
        "ORDER.INVALID_PAYMENT_AMOUNT",
        "Số tiền thanh toán phải lớn hơn 0 và phù hợp decimal(18,2).");

    public static readonly Error NoPaymentsProvided = new(
        ErrorType.Invalid,
        "ORDER.NO_PAYMENTS_PROVIDED",
        "Vui lòng cung cấp ít nhất một phương thức thanh toán.");

    public static readonly Error TransactionRefRequired = new(
        ErrorType.Invalid,
        "ORDER.TRANSACTION_REF_REQUIRED",
        "Thanh toán điện tử yêu cầu mã tham chiếu giao dịch.");

    public static readonly Error PointsRequireCustomer = new(
        ErrorType.Invalid,
        "ORDER.POINTS_REQUIRE_CUSTOMER",
        "Thanh toán bằng điểm yêu cầu đơn hàng có khách hàng.");

    public static readonly Error InsufficientPoints = new(
        ErrorType.Invalid,
        "ORDER.INSUFFICIENT_POINTS",
        "Tài khoản điểm không đủ để thanh toán.");

    public static readonly Error NonCashOverpaymentNotAllowed = new(
        ErrorType.Invalid,
        "ORDER.NON_CASH_OVERPAYMENT",
        "Phương thức thanh toán không dùng tiền mặt không được vượt số tiền còn lại.");

    public static readonly Error StockInsufficient = new(
        ErrorType.Invalid,
        "ORDER.STOCK_INSUFFICIENT",
        "Tồn kho không đủ để hoàn tất đơn hàng.");

    public static readonly Error GrandTotalMismatch = new(
        ErrorType.Invalid,
        "ORDER.GRAND_TOTAL_MISMATCH",
        "Tổng tiền đơn hàng đã thay đổi. Vui lòng tải lại đơn và xác nhận lại.");

    public static readonly Error ShiftStoreMismatch = new(
        ErrorType.Invalid,
        "ORDER.SHIFT_STORE_MISMATCH",
        "Ca làm việc không thuộc cửa hàng của đơn hàng.");

    public static readonly Error ShiftNotOwned = new(
        ErrorType.Forbidden,
        "ORDER.SHIFT_NOT_OWNED",
        "Cashier chỉ được thanh toán trong ca làm việc của chính mình.");

    public static readonly Error StoreInactive = new(
        ErrorType.Invalid,
        "ORDER.STORE_INACTIVE",
        "Cửa hàng không còn hoạt động.");

    public static readonly Error NotConfirmed = new(
        ErrorType.Invalid,
        "ORDER.NOT_CONFIRMED",
        "Đơn hàng chưa được xác nhận.");
}
