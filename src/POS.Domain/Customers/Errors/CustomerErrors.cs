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
}
