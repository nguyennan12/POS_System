using Microsoft.EntityFrameworkCore;
using POS.Application.Abstractions.Persistence;
using POS.Domain.Promotions;

namespace POS.Infrastructure.Persistence.Repositories;

public class VoucherRepository(AppDbContext dbContext) : IVoucherRepository
{
    public async Task<Voucher?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await dbContext.Vouchers
            .FirstOrDefaultAsync(v => v.Id == id, cancellationToken);
    }

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

    public async Task<bool> IsCodeUniqueAsync(string code, Guid? excludeVoucherId = null, CancellationToken cancellationToken = default)
    {
        var normalizedCode = code.Trim().ToUpper();
        return !await dbContext.Vouchers
            .AnyAsync(v => v.Code.ToUpper() == normalizedCode && (!excludeVoucherId.HasValue || v.Id != excludeVoucherId.Value), cancellationToken);
    }

    public async Task<int> GetCustomerUsageCountAsync(Guid voucherId, Guid customerId, CancellationToken cancellationToken = default)
    {
        return await dbContext.VoucherUsages
            .CountAsync(u => u.VoucherId == voucherId && u.CustomerId == customerId, cancellationToken);
    }

    public async Task<(List<Voucher> Items, int TotalCount)> GetPagedAsync(
        string? code,
        bool? isActive,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Vouchers.AsNoTracking().Include(v => v.Promotion).AsQueryable();

        if (!string.IsNullOrWhiteSpace(code))
        {
            var trimmed = code.Trim().ToUpper();
            query = query.Where(v => v.Code.ToUpper().Contains(trimmed));
        }

        if (isActive.HasValue)
        {
            query = query.Where(v => v.IsActive == isActive.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var pNum = pageNumber < 1 ? 1 : pageNumber;
        var pSize = pageSize < 1 ? 20 : pageSize;

        var items = await query
            .OrderBy(v => v.Code)
            .Skip((pNum - 1) * pSize)
            .Take(pSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<(List<Voucher> Items, int TotalCount)> GetPagedByPromotionIdAsync(
        Guid promotionId,
        string? code,
        bool? isActive,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Vouchers.AsNoTracking()
            .Include(v => v.Promotion)
            .Where(v => v.PromotionId == promotionId);

        if (!string.IsNullOrWhiteSpace(code))
        {
            var trimmed = code.Trim().ToUpper();
            query = query.Where(v => v.Code.ToUpper().Contains(trimmed));
        }

        if (isActive.HasValue)
        {
            query = query.Where(v => v.IsActive == isActive.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var pNum = pageNumber < 1 ? 1 : pageNumber;
        var pSize = pageSize < 1 ? 20 : pageSize;

        var items = await query
            .OrderBy(v => v.Code)
            .Skip((pNum - 1) * pSize)
            .Take(pSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task AddAsync(Voucher voucher, CancellationToken cancellationToken = default)
    {
        await dbContext.Vouchers.AddAsync(voucher, cancellationToken);
    }

    public void Remove(Voucher voucher)
    {
        dbContext.Vouchers.Remove(voucher);
    }
}
