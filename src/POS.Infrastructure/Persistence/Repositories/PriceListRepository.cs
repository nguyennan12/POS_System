using Microsoft.EntityFrameworkCore;
using POS.Application.Abstractions.Persistence;
using POS.Domain.Products;

namespace POS.Infrastructure.Persistence.Repositories;

internal sealed class PriceListRepository : IPriceListRepository
{
    private readonly AppDbContext _context;

    public PriceListRepository(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Kiểm tra chồng lấn thời gian bảng giá, xử lý đúng NULL CustomerGroup bằng biểu thức điều kiện riêng,
    /// đồng thời lọc theo StoreId.
    /// </summary>
    public async Task<bool> IsOverlappingAsync(
        Guid skuId,
        Guid storeId,
        string? customerGroup,
        DateTime validFrom,
        DateTime? validTo,
        CancellationToken cancellationToken = default)
    {
        return await _context.PriceLists
            .Where(p =>
                p.SkuId == skuId &&
                p.StoreId == storeId &&
                // NULL-safe customerGroup comparison: EF translates to IS NULL / = correctly
                (customerGroup == null ? p.CustomerGroup == null : p.CustomerGroup == customerGroup))
            .AnyAsync(p =>
                (p.ValidTo == null || p.ValidTo > validFrom) &&
                (validTo == null || validTo > p.ValidFrom),
                cancellationToken);
    }

    public async Task AddAsync(PriceList priceList, CancellationToken cancellationToken = default)
    {
        await _context.PriceLists.AddAsync(priceList, cancellationToken);
    }
}
