namespace POS.Contracts.V1.Employees;

public record EmployeeResponse(
    Guid Id,
    string Name,
    string Username,
    Guid RoleId,
    string RoleName,
    Guid? StoreId,
    string? StoreName,
    bool IsChainOwner,
    bool IsActive,
    DateTimeOffset CreatedAt,
    string? Phone = null
);

public record EmployeeDetailResponse(
    Guid Id,
    string Name,
    string Username,
    Guid RoleId,
    string RoleName,
    Guid? StoreId,
    string? StoreName,
    bool IsChainOwner,
    bool IsActive,
    short FailedLoginCount,
    DateTimeOffset? LockedUntil,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string? Phone = null, string? Email = null, string? CitizenId = null, DateOnly? HireDate = null, string? EmploymentType = null, decimal? HourlyWage = null
);

public record LoginHistoryResponse(
    Guid Id,
    string Action,
    string? Description,
    string? IpAddress,
    DateTimeOffset CreatedAt
);
