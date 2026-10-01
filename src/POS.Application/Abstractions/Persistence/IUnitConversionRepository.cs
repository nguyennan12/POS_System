using POS.Domain.Products;

namespace POS.Application.Abstractions.Persistence;

public interface IUnitConversionRepository
{
    Task<UnitConversion?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> IsUnitNameUniqueAsync(Guid skuId, string unitName, Guid? excludeId = null, CancellationToken cancellationToken = default);
    Task AddAsync(UnitConversion unitConversion, CancellationToken cancellationToken = default);
}
