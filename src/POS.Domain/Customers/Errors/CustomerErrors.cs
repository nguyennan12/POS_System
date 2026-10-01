using POS.Domain.Common;

namespace POS.Domain.Customers.Errors;

public static class CustomerErrors
{
    public static readonly Error NotFound = new(
        ErrorType.NotFound,
        "CUSTOMER.NOT_FOUND",
        "Khách hàng không tồn tại trên hệ thống.");

    public static readonly Error DuplicatePhone = new(
        ErrorType.AlreadyExists,
        "CUSTOMER.DUPLICATE_PHONE",
        "Số điện thoại đã tồn tại cho một khách hàng khác.");

    public static readonly Error DuplicateBarcode = new(
        ErrorType.AlreadyExists,
        "CUSTOMER.DUPLICATE_BARCODE",
        "Mã thẻ/barcode đã tồn tại cho một khách hàng khác.");

    public static readonly Error MemberTierNotFound = new(
        ErrorType.NotFound,
        "MEMBER_TIER.NOT_FOUND",
        "Hạng thành viên không tồn tại.");

    public static readonly Error Inactive = new(
        ErrorType.Invalid,
        "CUSTOMER.INACTIVE",
        "Khách hàng đang ở trạng thái ngưng hoạt động.");

    public static readonly Error HasOrdersCannotDelete = new(
        ErrorType.Invalid,
        "CUSTOMER.HAS_ORDERS",
        "Khách hàng đã phát sinh đơn hàng, không thể xóa khỏi hệ thống.");

    public static readonly Error MemberTierDuplicateMinSpending = new(
        ErrorType.AlreadyExists,
        "MEMBER_TIER.DUPLICATE_MIN_SPENDING",
        "Các hạng thành viên không được có cùng mức chi tiêu tối thiểu (minSpending).");

    public static readonly Error MemberTierInvalidOrder = new(
        ErrorType.Validation,
        "MEMBER_TIER.INVALID_ORDER",
        "Mức chi tiêu tối thiểu của hạng cao hơn phải lớn hơn hạng thấp hơn theo thứ tự phân cấp (Normal < Silver < Gold < VIP).");

    public static readonly Error InsufficientPoints = new(
        ErrorType.Validation,
        "LOYALTY.INSUFFICIENT_POINTS",
        "Số điểm thưởng trong tài khoản không đủ để thực hiện giao dịch.");

    public static readonly Error InvalidPoints = new(
        ErrorType.Validation,
        "LOYALTY.INVALID_POINTS",
        "Số điểm không hợp lệ.");

    public static readonly Error LoyaltyAccountNotFound = new(
        ErrorType.NotFound,
        "LOYALTY.ACCOUNT_NOT_FOUND",
        "Tài khoản tích điểm không tồn tại.");
}
