using POS.Domain.StoreLayouts.Picking;

namespace POS.Application.Abstractions.Persistence;

public interface IPickingOrderRepository
{
    Task<PickingOrder?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    void Add(PickingOrder order);
}
