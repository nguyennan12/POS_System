using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using POS.Domain.Stores;

namespace POS.Application.Abstractions.Persistence;

public interface IPosRegisterRepository
{
    Task<PosRegister?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PosRegister>> GetByStoreIdAsync(Guid storeId, CancellationToken cancellationToken = default);
    Task<PosRegister?> GetByCodeAsync(Guid storeId, string code, CancellationToken cancellationToken = default);
    Task AddAsync(PosRegister register, CancellationToken cancellationToken = default);
}
