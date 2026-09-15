using POS.Domain.Common;

namespace POS.Domain.Employees;

public static class ShiftErrors
{
    public static readonly Error AlreadyOpen = new(
        ErrorType.AlreadyExists,
        "Shift.AlreadyOpen",
        "Cửa hàng đang có ca làm việc mở. Vui lòng đóng ca trước khi mở ca mới.");

    public static readonly Error NotFound = new(
        ErrorType.NotFound,
        "Shift.NotFound",
        "Không tìm thấy ca làm việc.");

    public static readonly Error AlreadyClosed = new(
        ErrorType.Invalid,
        "Shift.AlreadyClosed",
        "Ca làm việc đã được đóng.");
}
