using Microsoft.EntityFrameworkCore;
using POS.Application.Abstractions.Persistence;
using POS.Domain.StoreLayouts.Picking;

namespace POS.Infrastructure.Persistence.Repositories;

public class PickingOrderRepository(AppDbContext dbContext) : IPickingOrderRepository
{
    public async Task<PickingOrder?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await dbContext.PickingOrders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
    }

    public void Add(PickingOrder order) => dbContext.PickingOrders.Add(order);
}
