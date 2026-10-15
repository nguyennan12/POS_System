using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;
using POS.Contracts.V1.Config;

namespace POS.Application.UseCases.Config.Queries.GetStoreConfig;

public record GetStoreConfigQuery(Guid? StoreId) : IQuery<StoreConfigResponse>, IRequirePermission
{
    public string RequiredPermission => "stores:read";
}
