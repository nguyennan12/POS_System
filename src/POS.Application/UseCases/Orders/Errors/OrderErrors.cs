using POS.Domain.Common;

namespace POS.Application.UseCases.Orders.Errors;

public static class OrderErrors
{
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
        "Giỏ hàng đang trống. Vui lòng thêm sản phẩm vào giỏ trước khi áp dụng voucher.");
}
