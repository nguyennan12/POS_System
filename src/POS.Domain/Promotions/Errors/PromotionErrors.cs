using POS.Domain.Common;

namespace POS.Domain.Promotions.Errors;

public static class PromotionErrors
{
    public static readonly Error NotFound = new(
        ErrorType.NotFound,
        "PROMOTION.NOT_FOUND",
        "Chương trình khuyến mãi không tồn tại trên hệ thống.");

    public static readonly Error InvalidDateRange = new(
        ErrorType.Validation,
        "PROMOTION.INVALID_DATE_RANGE",
        "Thời gian kết thúc khuyến mãi (validTo) phải sau thời gian bắt đầu (validFrom).");

    public static readonly Error InvalidValue = new(
        ErrorType.Validation,
        "PROMOTION.INVALID_VALUE",
        "Giá trị khuyến mãi phải lớn hơn 0 và không vượt quá 100% nếu là giảm theo phần trăm.");

    public static readonly Error TargetRequired = new(
        ErrorType.Validation,
        "PROMOTION.TARGET_REQUIRED",
        "Khuyến mãi áp dụng theo Category hoặc SKU yêu cầu ít nhất một danh mục hoặc sản phẩm mục tiêu.");

    public static readonly Error VoucherNotFound = new(
        ErrorType.NotFound,
        "VOUCHER.NOT_FOUND",
        "Mã voucher không tồn tại.");

    public static readonly Error VoucherDuplicateCode = new(
        ErrorType.AlreadyExists,
        "VOUCHER.DUPLICATE_CODE",
        "Mã voucher đã tồn tại trên hệ thống.");

    public static readonly Error VoucherExpired = new(
        ErrorType.Invalid,
        "VOUCHER.EXPIRED",
        "Mã voucher đã hết hạn sử dụng.");

    public static readonly Error VoucherMaxUsesReached = new(
        ErrorType.Invalid,
        "VOUCHER.MAX_USES_REACHED",
        "Mã voucher đã hết lượt sử dụng.");

    public static readonly Error VoucherCustomerLimitReached = new(
        ErrorType.Invalid,
        "VOUCHER.CUSTOMER_LIMIT_REACHED",
        "Khách hàng đã dùng hết số lượt cho phép với mã giảm giá này.");

    public static readonly Error VoucherInactive = new(
        ErrorType.Invalid,
        "VOUCHER.INACTIVE",
        "Mã voucher đang bị khóa.");

    public static readonly Error PromotionInactive = new(
        ErrorType.Invalid,
        "PROMOTION.INACTIVE",
        "Chương trình khuyến mãi liên kết đang không hoạt động hoặc chưa đến thời gian áp dụng.");

    public static readonly Error MinOrderAmountNotMet = new(
        ErrorType.Invalid,
        "PROMOTION.MIN_ORDER_NOT_MET",
        "Đơn hàng chưa đạt giá trị tối thiểu để áp dụng khuyến mãi.");
}
