using POS.Domain.Common;

namespace POS.Application.UseCases.Orders.Access;

public sealed record OrderReadScope(
    Guid StoreId,
    Guid EmployeeId,
    bool CanReadAllInStore);

/// <summary>
/// Chính sách kiểm tra quyền đọc đơn hàng dùng chung cho các công cụ AI và truy vấn nội bộ:
/// - Người dùng có <c>orders:read</c> được đọc toàn bộ đơn thuộc chi nhánh hiện tại.
/// - Thu ngân chỉ có <c>orders:read_own</c> chỉ được đọc đơn do chính <c>EmployeeId</c> của mình tạo.
/// </summary>
public interface IOrderReadAccessPolicy
{
    Task<Result<OrderReadScope>> ResolveReadScopeAsync(CancellationToken cancellationToken = default);
}
