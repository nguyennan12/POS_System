using POS.Domain.Common;

namespace POS.Domain.Rbac.Errors;

public static class RoleErrors
{
    public static readonly Error NotFound = new(ErrorType.NotFound, "Role.NotFound", "Vai trò không tồn tại.");
    public static readonly Error SystemRoleCannotBeModified = new(ErrorType.Forbidden, "Role.SystemRoleCannotBeModified", "Không thể chỉnh sửa hoặc thay đổi quyền của vai trò hệ thống.");
    public static readonly Error NameAlreadyExists = new(ErrorType.AlreadyExists, "Role.NameAlreadyExists", "Tên vai trò đã tồn tại trong phạm vi cửa hàng.");
    public static readonly Error InvalidStore = new(ErrorType.Invalid, "Role.InvalidStore", "Cửa hàng không tồn tại hoặc không hợp lệ.");
    public static readonly Error InvalidPermissionIds = new(ErrorType.Invalid, "Role.InvalidPermissionIds", "Một hoặc nhiều mã quyền không tồn tại trong hệ thống.");
}
