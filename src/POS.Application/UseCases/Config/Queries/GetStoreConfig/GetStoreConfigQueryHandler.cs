using POS.Application.Abstractions.Messaging;
using POS.Application.Abstractions.Persistence;
using POS.Contracts.V1.Config;
using POS.Domain.Common;

namespace POS.Application.UseCases.Config.Queries.GetStoreConfig;

public class GetStoreConfigQueryHandler(ISystemConfigRepository configs)
    : IQueryHandler<GetStoreConfigQuery, StoreConfigResponse>
{
    public async Task<Result<StoreConfigResponse>> Handle(
        GetStoreConfigQuery query, CancellationToken cancellationToken)
    {
        var list = await configs.GetByStoreAsync(query.StoreId, cancellationToken);
        var dict = list.ToDictionary(c => c.Key, c => c.Value);
        return new StoreConfigResponse(query.StoreId, dict);
    }
}
