using POS.Contracts.V1.Common;

namespace POS.Contracts.V1.Employees;

public record EmployeeFilterRequest(
    Guid? StoreId = null,
    Guid? RoleId = null,
    string? Search = null,
    bool? IsActive = null,
    int PageNumber = 1,
    int PageSize = 20
) : PagedRequest(PageNumber, PageSize);

public record CreateEmployeeRequest(
    string Name,
    string Username,
    string Password,
    string Pin,
    Guid RoleId,
    Guid? StoreId = null,
    bool IsChainOwner = false,
    string? Phone = null, string? Email = null, string? CitizenId = null, DateOnly? HireDate = null, string? EmploymentType = null, decimal? HourlyWage = null
);

public record UpdateEmployeeRequest(
    string Name,
    Guid RoleId,
    Guid? StoreId = null,
    bool IsChainOwner = false,
    string? Phone = null, string? Email = null, string? CitizenId = null, DateOnly? HireDate = null, string? EmploymentType = null, decimal? HourlyWage = null
);

public record LockEmployeeRequest(
    bool IsActive
);

public record ResetPasswordRequest(
    string NewPassword
);

public record ResetPinRequest(
    string NewPin
);
