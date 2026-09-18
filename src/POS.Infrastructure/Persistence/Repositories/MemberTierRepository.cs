using Microsoft.EntityFrameworkCore;
using POS.Application.Abstractions.Persistence;
using POS.Domain.Customers;
using POS.Domain.Customers.Enums;

namespace POS.Infrastructure.Persistence.Repositories;

public class MemberTierRepository(AppDbContext context) : IMemberTierRepository
{
    public Task<List<MemberTier>> GetAllAsync(CancellationToken cancellationToken = default) =>
        context.MemberTiers
            .AsNoTracking()
            .OrderByDescending(t => t.MinSpending)
            .ToListAsync(cancellationToken);

    public Task<MemberTier?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.MemberTiers.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

    public Task<MemberTier?> GetByNameAsync(MemberTierName name, CancellationToken cancellationToken = default) =>
        context.MemberTiers.FirstOrDefaultAsync(t => t.Name == name, cancellationToken);

    public Task<MemberTier?> GetDefaultTierAsync(CancellationToken cancellationToken = default) =>
        context.MemberTiers.FirstOrDefaultAsync(t => t.Name == MemberTierName.Normal, cancellationToken);
}
