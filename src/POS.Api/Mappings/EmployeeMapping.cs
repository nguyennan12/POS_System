using POS.Application.UseCases.Employees;
using POS.Contracts.V1.Employees;

namespace POS.Api.Mappings;

public static class EmployeeMapping
{
    public static EmployeeResponse ToResponse(this EmployeeDto e) => new(e.Id, e.Name, e.Username,
        e.RoleId, e.RoleName, e.StoreId, e.StoreName, e.IsChainOwner, e.IsActive, e.CreatedAt);
    public static EmployeeDetailResponse ToDetailResponse(this EmployeeDto e) => new(e.Id, e.Name, e.Username,
        e.RoleId, e.RoleName, e.StoreId, e.StoreName, e.IsChainOwner, e.IsActive, e.FailedLoginCount,
        e.LockedUntil, e.CreatedAt, e.UpdatedAt);
    public static LoginHistoryResponse ToResponse(this EmployeeLoginHistoryDto a) =>
        new(a.Id, a.Action, a.Description, a.IpAddress, a.CreatedAt);
}
