using POS.Domain.Employees;

namespace POS.Application.Abstractions.Persistence;

public interface IEmployeeRepository
{
  Task<Employee?> GetByUsernameWithRoleAndStoreAsync(string username, CancellationToken cancellationToken = default);
  Task<Employee?> GetByStoreAndPinLookupHashWithRoleAndStoreAsync(Guid storeId, string pinLookupHash, CancellationToken cancellationToken = default);
}
