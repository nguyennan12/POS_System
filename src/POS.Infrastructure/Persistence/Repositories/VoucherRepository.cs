using Microsoft.EntityFrameworkCore;
using POS.Application.Abstractions.Persistence;
using POS.Domain.Promotions;

namespace POS.Infrastructure.Persistence.Repositories;

public class VoucherRepository(AppDbContext dbContext) : IVoucherRepository
{
    public async Task<Voucher?> GetByIdWithPromotionAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await dbContext.Vouchers
            .Include(v => v.Promotion)
                .ThenInclude(p => p.Targets)
            .FirstOrDefaultAsync(v => v.Id == id, cancellationToken);
    }

    public async Task<Voucher?> GetByCodeWithPromotionAsync(string code, CancellationToken cancellationToken = default)
    {
        return await dbContext.Vouchers
            .Include(v => v.Promotion)
                .ThenInclude(p => p.Targets)
            .FirstOrDefaultAsync(v => v.Code.ToUpper() == code.Trim().ToUpper(), cancellationToken);
    }

    public async Task<int> GetCustomerUsageCountAsync(Guid voucherId, Guid customerId, CancellationToken cancellationToken = default)
    {
        return await dbContext.VoucherUsages
            .CountAsync(u => u.VoucherId == voucherId && u.CustomerId == customerId, cancellationToken);
    }
}
