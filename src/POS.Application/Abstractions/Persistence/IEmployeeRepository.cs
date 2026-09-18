using POS.Domain.Employees;

namespace POS.Application.Abstractions.Persistence;

public interface IEmployeeRepository
{
    Task AddAsync(Employee employee, CancellationToken cancellationToken = default);
    Task<Employee?> GetDetailAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExistsByUsernameAsync(string username, CancellationToken cancellationToken = default);
    Task<int> CountActiveOwnersInStoreAsync(Guid? storeId, CancellationToken cancellationToken = default);
    Task<(List<Employee> Items, int TotalCount)> GetPagedAsync(Guid? storeId, Guid? employeeId,
        Guid? roleId, string? search, bool? isActive, int pageNumber, int pageSize,
        CancellationToken cancellationToken = default);
    Task<Employee?> GetByUsernameWithRoleAndStoreAsync(string username, CancellationToken cancellationToken = default);
    Task<Employee?> GetByStoreAndPinLookupHashWithRoleAndStoreAsync(Guid storeId, string pinLookupHash, CancellationToken cancellationToken = default);
    Task<Employee?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> HasPinConflictAsync(Guid employeeId, Guid storeId, string pinLookupHash, CancellationToken cancellationToken = default);
    Task<bool> HasMissingPinLookupAsync(Guid storeId, CancellationToken cancellationToken = default);
    Task<bool> HasOpenShiftOutsideStoreAsync(Guid employeeId, Guid storeId, CancellationToken cancellationToken = default);
}
