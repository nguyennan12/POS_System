using POS.Application.UseCases.StoreLayouts;
using POS.Domain.StoreLayouts;
using POS.Domain.StoreLayouts.Enums;

namespace POS.Application.Abstractions.Persistence;

public interface IStoreLayoutRepository
{
    Task<StoreLayout?> GetByIdAsync(
        Guid id,
        bool includeGraph = false,
        CancellationToken cancellationToken = default);

    Task<StoreLayout?> GetForUpdateAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<StoreLayout?> GetActiveByStoreAsync(
        Guid storeId,
        bool includeGraph = false,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<StoreLayoutSummaryDto> Items, int Total)> ListAsync(
        Guid storeId,
        StoreLayoutStatus? status,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<int> GetNextVersionNumberAsync(
        Guid storeId,
        CancellationToken cancellationToken = default);

    Task<bool> HasActiveLayoutAsync(
        Guid storeId,
        CancellationToken cancellationToken = default);

    void Add(StoreLayout layout);
    void Remove(StoreLayout layout);
}
