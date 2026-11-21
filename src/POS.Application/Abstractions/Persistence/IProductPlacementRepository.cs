using POS.Application.UseCases.StoreLayouts;
using POS.Domain.StoreLayouts;

namespace POS.Application.Abstractions.Persistence;

public interface IProductPlacementRepository
{
    Task<ProductPlacement?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<ProductLocationsDto> GetActiveLocationsAsync(
        Guid storeId,
        Guid skuId,
        CancellationToken cancellationToken = default);

    Task<ProductLocationsDto> GetLocationsByLayoutAsync(
        Guid layoutId,
        Guid skuId,
        CancellationToken cancellationToken = default);

    Task<ShelfContentsDto?> GetShelfContentsAsync(
        Guid storeId,
        Guid shelfId,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<PlacementHistoryDto> Items, int Total)> GetHistoryAsync(
        Guid storeId,
        Guid skuId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<List<ProductPlacement>> GetPlacementsByShelfLevelIdAsync(
        Guid shelfLevelId,
        CancellationToken cancellationToken = default);

    Task<List<ProductPlacement>> GetPlacementsByLayoutIdAsync(
        Guid layoutId,
        CancellationToken cancellationToken = default);

    void Add(ProductPlacement placement);
    void Remove(ProductPlacement placement);
    void AddHistory(ProductPlacementHistory history);
}
