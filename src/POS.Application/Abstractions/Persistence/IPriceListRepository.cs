using POS.Domain.Products;

namespace POS.Application.Abstractions.Persistence;

public interface IPriceListRepository
{
    Task<bool> IsOverlappingAsync(Guid skuId, string? customerGroup, DateTime validFrom, DateTime? validTo, CancellationToken cancellationToken = default);
    Task AddAsync(PriceList priceList, CancellationToken cancellationToken = default);
}
