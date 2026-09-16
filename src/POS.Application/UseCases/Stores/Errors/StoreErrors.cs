using POS.Domain.Common;

namespace POS.Application.UseCases.Stores.Errors;

public static class StoreErrors
{
    public static readonly Error Unauthorized = new(
        ErrorType.Unauthorized,
        "STORE.UNAUTHORIZED",
        "Cần đăng nhập với danh tính nhân viên hợp lệ.");

    public static readonly Error Forbidden = new(
        ErrorType.Forbidden,
        "STORE.FORBIDDEN",
        "Không có quyền quản lý cửa hàng này.");

    public static readonly Error StoreNotFound = new(
        ErrorType.NotFound,
        "STORE.NOT_FOUND",
        "Store không tồn tại.");

    public static readonly Error EmployeeNotFound = new(
        ErrorType.NotFound,
        "STORE.EMPLOYEE_NOT_FOUND",
        "Employee không tồn tại.");

    public static readonly Error InactiveStore = new(
        ErrorType.Invalid,
        "STORE.INACTIVE",
        "StoreStatus không hợp lệ.");

    public static readonly Error InactiveEmployee = new(
        ErrorType.Invalid,
        "STORE.EMPLOYEE_INACTIVE",
        "EmployeeStatus không hợp lệ.");

    public static readonly Error InvalidEmployeeRole = new(
        ErrorType.Invalid,
        "STORE.INVALID_EMPLOYEE_ROLE",
        "EmployeeRole không hợp lệ.");

    public static readonly Error InvalidEmployeeStore = new(
        ErrorType.Invalid,
        "STORE.INVALID_EMPLOYEE_STORE",
        "EmployeeStore không hợp lệ.");

    public static readonly Error InvalidStoreManagerRole = new(
        ErrorType.Invalid,
        "STORE.INVALID_MANAGER_ROLE",
        "StoreManagerRole không hợp lệ.");

    public static readonly Error EmployeeOpenShift = new(
        ErrorType.Invalid,
        "STORE.EMPLOYEE_OPEN_SHIFT",
        "EmployeeOpenShift không hợp lệ.");

    public static readonly Error PinLookupMissing = new(
        ErrorType.Invalid,
        "STORE.PIN_LOOKUP_MISSING",
        "Chưa đủ dữ liệu PIN lookup để kiểm tra trùng PIN. Cần hoàn thiện/reset PIN qua T12/T20 trước khi chuyển cửa hàng.");

    public static readonly Error EmployeePinAlreadyExists = new(
        ErrorType.AlreadyExists,
        "STORE.EMPLOYEE_PIN_ALREADY_EXISTS",
        "EmployeePin đã tồn tại.");

    public static readonly Error OwnerAccessRecipientInvalid = new(
        ErrorType.Invalid,
        "STORE.INVALID_OWNER_ACCESS_RECIPIENT",
        "OwnerAccessRecipient không hợp lệ.");

    public static readonly Error EmployeeStoreAccessAlreadyExists = new(
        ErrorType.AlreadyExists,
        "STORE.EMPLOYEE_ACCESS_ALREADY_EXISTS",
        "EmployeeStoreAccess đã tồn tại.");
}
