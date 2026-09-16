using POS.Domain.Customers;
using POS.Domain.Customers.Enums;

namespace POS.Application.Abstractions.Persistence;

public interface IMemberTierRepository
{
    Task<List<MemberTier>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<MemberTier?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<MemberTier?> GetByNameAsync(MemberTierName name, CancellationToken cancellationToken = default);
    Task<MemberTier?> GetDefaultTierAsync(CancellationToken cancellationToken = default);
}
