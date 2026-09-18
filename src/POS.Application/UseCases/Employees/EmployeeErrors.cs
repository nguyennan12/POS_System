using POS.Domain.Common;

namespace POS.Application.UseCases.Employees;

public static class EmployeeErrors
{
    public static readonly Error Forbidden = new(ErrorType.Forbidden, "Employee.Forbidden", "Không đủ quyền quản lý nhân viên này.");
    public static readonly Error TransferForbidden = new(ErrorType.Forbidden, "Employee.TransferForbidden", "Chỉ được chuyển cửa hàng cho Cashier hoặc StoreManager.");
    public static readonly Error LastOwner = new(ErrorType.Forbidden, "Employee.LastActiveOwner", "Không được khóa Owner/Chain Owner active cuối cùng của cửa hàng.");
    public static readonly Error NotFound = new(ErrorType.NotFound, "Employee.NotFound", "Nhân viên không tồn tại.");
    public static readonly Error InvalidRole = new(ErrorType.Invalid, "Employee.InvalidRole", "Role không tồn tại hoặc không thuộc cửa hàng đã chọn.");
    public static readonly Error InvalidStore = new(ErrorType.Invalid, "Employee.InvalidStore", "Cần cửa hàng hợp lệ, đang hoạt động.");
    public static readonly Error UsernameExists = new(ErrorType.AlreadyExists, "Employee.UsernameExists", "Tên đăng nhập đã tồn tại.");
    public static readonly Error PinExists = new(ErrorType.AlreadyExists, "Employee.PinExists", "PIN đã tồn tại trong cửa hàng.");
}
