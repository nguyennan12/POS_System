using POS.Application.Abstractions.Persistence;
using POS.Domain.Promotions;

namespace POS.Infrastructure.Persistence.Repositories;

public class VoucherUsageRepository(AppDbContext dbContext) : IVoucherUsageRepository
{
    public async Task AddAsync(VoucherUsage usage, CancellationToken cancellationToken = default)
    {
        await dbContext.VoucherUsages.AddAsync(usage, cancellationToken);
    }
}
