using POS.Domain.Employees;
using POS.Domain.Auditing;

namespace POS.Application.UseCases.Employees;

public record EmployeeDto(Guid Id, string Name, string Username, Guid RoleId, string RoleName,
    Guid? StoreId, string? StoreName, bool IsChainOwner, bool IsActive, short FailedLoginCount,
    DateTimeOffset? LockedUntil, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt,
    string? Phone = null, string? Email = null, string? CitizenId = null, DateOnly? HireDate = null, string? EmploymentType = null, decimal? HourlyWage = null);

public record EmployeeLoginHistoryDto(Guid Id, string Action, string? Description,
    string? IpAddress, DateTimeOffset CreatedAt);

public record PagedEmployees(List<EmployeeDto> Items, int TotalCount, int PageNumber, int PageSize);
public record PagedEmployeeLoginHistory(List<EmployeeLoginHistoryDto> Items, int TotalCount, int PageNumber, int PageSize);

public static class EmployeeDtoExtensions
{
    public static EmployeeDto ToDto(this Employee e) => new(e.Id, e.Name, e.Username, e.RoleId,
        e.Role.Name, e.StoreId, e.Store?.Name, e.IsChainOwner, e.IsActive, e.FailedLoginCount,
        e.LockedUntil, e.CreatedAt, e.UpdatedAt, e.Phone, e.Email, e.CitizenId, e.HireDate, e.EmploymentType, e.HourlyWage);
    public static EmployeeLoginHistoryDto ToDto(this AuditLog a) =>
        new(a.Id, a.Action, a.Description, a.IpAddress, a.CreatedAt);
}
