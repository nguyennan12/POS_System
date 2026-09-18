using POS.Domain.Employees;

namespace POS.Application.Abstractions.Persistence;

public interface IEmployeeStoreAccessRepository
{
    Task RemoveAllByEmployeeIdAsync(Guid employeeId, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid employeeId, Guid storeId, CancellationToken cancellationToken = default);
    Task AddAsync(EmployeeStoreAccess access, CancellationToken cancellationToken = default);
}
