namespace POS.Contracts.V1.Employees;

public record EmployeeStoreOption(Guid Id, string Name, bool IsActive);
public record EmployeeRoleOption(Guid Id, string Name, Guid? StoreId, int Level, bool IsSystemRole,
    IReadOnlyList<string> Permissions);
public record EmployeeStatusOption(bool IsActive, string Name);
public record EmployeeOptionsResponse(IReadOnlyList<EmployeeStoreOption> Stores,
    IReadOnlyList<EmployeeRoleOption> Roles, IReadOnlyList<EmployeeStatusOption> Statuses);
