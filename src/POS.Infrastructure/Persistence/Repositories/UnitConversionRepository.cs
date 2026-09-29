using Microsoft.EntityFrameworkCore;
using POS.Application.Abstractions.Persistence;
using POS.Domain.Products;

namespace POS.Infrastructure.Persistence.Repositories;

internal sealed class UnitConversionRepository : IUnitConversionRepository
{
    private readonly AppDbContext _context;

    public UnitConversionRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<UnitConversion?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.UnitConversions.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    public async Task AddAsync(UnitConversion unitConversion, CancellationToken cancellationToken = default)
    {
        await _context.UnitConversions.AddAsync(unitConversion, cancellationToken);
    }
}
