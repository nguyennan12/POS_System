using POS.Application.Abstractions.Messaging;
using POS.Application.Abstractions.Persistence;
using POS.Application.UseCases.Stores.Queries.GetAllStores;
using POS.Domain.Common;

namespace POS.Application.UseCases.Stores.Queries.GetPublicStores;

public record GetPublicStoresQuery : IQuery<List<StoreDto>>;

public class GetPublicStoresQueryHandler(IStoreRepository stores)
    : IQueryHandler<GetPublicStoresQuery, List<StoreDto>>
{
    public async Task<Result<List<StoreDto>>> Handle(GetPublicStoresQuery query, CancellationToken cancellationToken)
    {
        var allStores = await stores.GetAllAsync(cancellationToken);
        var activeStores = allStores
            .Where(s => s.IsActive)
            .Select(s => new StoreDto(s.Id, s.Name, s.Address, s.Phone, s.IsActive))
            .ToList();
            
        return activeStores;
    }
}
