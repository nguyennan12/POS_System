using POS.Domain.Common;
using POS.Domain.Employees;
using POS.Domain.Rbac;

namespace POS.Application.Abstractions.Auth;

/// <summary>
/// Tập trung hóa quy tắc kiểm tra quyền Owner và phạm vi chi nhánh (Tenant Scope).
/// Tuân thủ Dependency Inversion Principle (DIP) và giúp các UseCase Handler
/// dễ dàng mock khi viết Unit Test.
/// </summary>
public interface IStoreAccessPolicy
{
    /// <summary>
    /// Xác thực người dùng hiện tại là Owner hợp lệ (IsActive, không bị khóa, có role Owner).
    /// Trả về Employee nếu hợp lệ, ngược lại trả về lỗi Unauthorized / Forbidden.
    /// </summary>
    Task<Result<Employee>> GetAuthenticatedOwnerAsync(CancellationToken ct = default);

    /// <summary>
    /// Kiểm tra người dùng hiện tại có thể truy cập chi nhánh <paramref name="targetStoreId"/>.
    /// ChainOwner luôn được phép; nhân viên chi nhánh chỉ được phép truy cập chi nhánh của mình.
    /// </summary>
    bool CanAccessStore(Guid? targetStoreId);

    /// <summary>
    /// Kiểm tra người dùng hiện tại có thể đọc / quản lý <paramref name="role"/>.
    /// Role hệ thống (IsSystemRole) hoặc không gắn chi nhánh thì ai cũng đọc được.
    /// </summary>
    bool CanAccessRole(Role role);
}
