using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;

namespace POS.Application.UseCases.Employees.Queries.GetEmployeeOptions;

public record GetEmployeeOptionsQuery : IQuery<EmployeeOptionsDto>, IRequirePermission
{
    public string RequiredPermission => "employees:read";
}

public record EmployeeStoreOptionDto(Guid Id, string Name, bool IsActive);
public record EmployeeRoleOptionDto(Guid Id, string Name, Guid? StoreId, int Level, bool IsSystemRole,
    IReadOnlyList<string> Permissions);
public record EmployeeStatusOptionDto(bool IsActive, string Name);
public record EmployeeOptionsDto(IReadOnlyList<EmployeeStoreOptionDto> Stores,
    IReadOnlyList<EmployeeRoleOptionDto> Roles, IReadOnlyList<EmployeeStatusOptionDto> Statuses);
