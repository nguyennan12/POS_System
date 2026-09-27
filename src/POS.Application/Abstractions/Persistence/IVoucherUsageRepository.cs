using POS.Domain.Promotions;

namespace POS.Application.Abstractions.Persistence;

public interface IVoucherUsageRepository
{
    Task AddAsync(VoucherUsage usage, CancellationToken cancellationToken = default);
}
