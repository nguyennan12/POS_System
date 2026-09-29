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

    public async Task<bool> IsOverlappingAsync(Guid skuId, string? customerGroup, DateTime validFrom, DateTime? validTo, CancellationToken cancellationToken = default)
    {
        return await _context.PriceLists
            .Where(p => p.SkuId == skuId && p.CustomerGroup == customerGroup)
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
