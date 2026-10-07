using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using POS.Application.Abstractions.Persistence;
using POS.Domain.Stores;

namespace POS.Infrastructure.Persistence.Repositories;

public class PosRegisterRepository(AppDbContext context) : IPosRegisterRepository
{
    public async Task<PosRegister?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await context.PosRegisters
            .Include(r => r.Store)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<PosRegister>> GetByStoreIdAsync(Guid storeId, CancellationToken cancellationToken = default)
    {
        return await context.PosRegisters
            .Where(r => r.StoreId == storeId && r.IsActive)
            .OrderBy(r => r.Code)
            .ToListAsync(cancellationToken);
    }

    public async Task<PosRegister?> GetByCodeAsync(Guid storeId, string code, CancellationToken cancellationToken = default)
    {
        return await context.PosRegisters
            .FirstOrDefaultAsync(r => r.StoreId == storeId && r.Code == code, cancellationToken);
    }

    public async Task AddAsync(PosRegister register, CancellationToken cancellationToken = default)
    {
        await context.PosRegisters.AddAsync(register, cancellationToken);
    }
}
