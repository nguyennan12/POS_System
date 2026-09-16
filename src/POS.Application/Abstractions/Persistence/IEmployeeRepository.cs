using POS.Domain.Employees;

namespace POS.Application.Abstractions.Persistence;

public interface IEmployeeRepository
{
    Task<Employee?> GetByUsernameWithRoleAndStoreAsync(string username, CancellationToken cancellationToken = default);
    Task<Employee?> GetByStoreAndPinLookupHashWithRoleAndStoreAsync(Guid storeId, string pinLookupHash, CancellationToken cancellationToken = default);
    Task<Employee?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> HasPinConflictAsync(Guid employeeId, Guid storeId, string pinLookupHash, CancellationToken cancellationToken = default);
    Task<bool> HasMissingPinLookupAsync(Guid storeId, CancellationToken cancellationToken = default);
    Task<bool> HasOpenShiftOutsideStoreAsync(Guid employeeId, Guid storeId, CancellationToken cancellationToken = default);
}